using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class LifestyleService
    {
        private readonly IUnitOfWork _unitOfWork;
        public LifestyleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //1.lấy tất cả câu hỏi về lối sống kèm theo các lựa chọn
        public async Task<IEnumerable<LifestyleQuestionDTO>> GetAllQuestionsWithOptionsAsync()
        {
            var questions = await _unitOfWork.LifestyleRepository.GetAllWithOptionsAsync();
            // dùng LINQ để chuyển đổi danh sách Entity sang danh sách DTO

            // Chuyển Entity sang DTO để ngắt hoàn toàn liên kết ngược
            var questionDtos = questions.Select(q => new LifestyleQuestionDTO
            {
                Id = q.Id,
                Content = q.Content,
                Options = q.Options.Select(o => new LifestyleOptionDTO
                {
                    Id = o.Id,
                    Content = o.Content
                }).ToList()
            });

            return questionDtos;
        }
        //2.Tạo câu hỏi mới kèm theo các lựa chọn
        public async Task CreateQuestionWithOptionsAsync(CreateQuestionDTO dto)
        {
            // Khởi tạo Entity LifestyleQuestion
            var newQuestion = new LifestyleQuestion
            {
                Content = dto.Content,

                // Map danh sách string thành danh sách Entity LifestyleOption
                Options = dto.Options.Select(optionText => new LifestyleOption
                {
                    Content = optionText
                }).ToList()
            };

            // Thêm vào DbSet thông qua Repository
            await _unitOfWork.Repository<LifestyleQuestion>().AddAsync(newQuestion);

            // Commit thay đổi xuống Database
            await _unitOfWork.CompleteAsync();
        }

        //3.USER: LƯU CÂU TRẢ LỜI CỦA MÌNH
        public async Task SubmitUserAnswersAsync(string userId, UserSubmitLifestyleDTO dto)
        {
            // 1. Loại bỏ các ID trùng lặp (phòng hờ Frontend gửi mảng lỗi kiểu [1, 1, 2])
            var uniqueOptionIds = dto.SelectedOptionIds.Distinct().ToList();

            // 2. Tìm options trong Database
            var selectedOptions = (await _unitOfWork.Repository<LifestyleOption>()
                .FindAsync(o => uniqueOptionIds.Contains(o.Id))).ToList();

            // 3. KIỂM TRA ĐIỀU KIỆN 1: OptionId phải tồn tại
            if (selectedOptions.Count != uniqueOptionIds.Count)
            {
                var foundIds = selectedOptions.Select(o => o.Id).ToList();
                var invalidIds = uniqueOptionIds.Except(foundIds).ToList();
                throw new ArgumentException($"Các OptionId sau không tồn tại: {string.Join(", ", invalidIds)}");
            }

            // ================== THÊM LOGIC KIỂM TRA SỐ LƯỢNG CÂU HỎI ==================

            // Lấy tổng số lượng câu hỏi đang có trong hệ thống
            var allQuestions = await _unitOfWork.Repository<LifestyleQuestion>().GetAllAsync();
            int totalQuestionsInDb = allQuestions.Count();

            // Lấy ra danh sách các ID câu hỏi mà User vừa trả lời (dùng Distinct để loại bỏ trùng lặp 
            // phòng trường hợp user chọn 2 đáp án cho cùng 1 câu hỏi)
            int answeredQuestionsCount = selectedOptions.Select(o => o.LifestyleQuestionId).Distinct().Count();

            // KIỂM TRA ĐIỀU KIỆN 2: Số câu trả lời phải bằng tổng số câu hỏi
            if (answeredQuestionsCount < totalQuestionsInDb)
            {
                throw new ArgumentException($"Vui lòng trả lời đầy đủ tất cả các câu hỏi. Hệ thống có {totalQuestionsInDb} câu, nhưng bạn mới trả lời {answeredQuestionsCount} câu.");
            }

            // KIỂM TRA ĐIỀU KIỆN 3 (Tùy chọn): Tránh trường hợp cố tình chọn 2 đáp án cho 1 câu hỏi
            if (uniqueOptionIds.Count > totalQuestionsInDb)
            {
                throw new ArgumentException("Số lượng đáp án vượt quá tổng số câu hỏi. Mỗi câu hỏi chỉ được chọn 1 đáp án.");
            }

            // ===========================================================================

            // 4. Xóa các câu trả lời cũ
            var oldAnswers = await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId == userId);

            foreach (var old in oldAnswers)
            {
                _unitOfWork.Repository<UserLifestyle>().Remove(old);
            }

            // 5. Thêm các câu trả lời mới
            foreach (var option in selectedOptions)
            {
                var newAnswer = new UserLifestyle
                {
                    UserId = userId,
                    LifestyleOptionId = option.Id,
                    LifestyleQuestionId = option.LifestyleQuestionId
                };
                await _unitOfWork.Repository<UserLifestyle>().AddAsync(newAnswer);
            }

            await _unitOfWork.CompleteAsync();
        }
        //4. Tính % phù hợp giữa 2 người dựa trên câu trả lời của họ (để gợi ý phòng ở phù hợp)
        public async Task<MatchingResultDTO> CalculateMatchingScoreAsync(string UserId, string targetUserId)
        {
            // Lấy toàn bộ câu trả lời của 2 user
            var answersA = await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId == UserId);

            var answersB = await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId == targetUserId);

            // Mặc dù UI đã chặn, Backend vẫn nên có 1 dòng check null phòng hờ dùng Postman gọi thẳng
            if (!answersA.Any() || !answersB.Any())
            {
                return new MatchingResultDTO
                {
                    TargetUserId = targetUserId,
                    MatchingScore = 0,
                    TotalQuestions = 0,
                    MatchedAnswers = 0
                };
            }

            //Lấy luôn tổng số câu hỏi dựa trên số đáp án (vì UI ép trả lời đủ)
            int totalQuestions = answersA.Count();

            //Đếm số đáp án giống hệt nhau (Phép giao Intersect)
            var optionsA = answersA.Select(x => x.LifestyleOptionId);
            var optionsB = answersB.Select(x => x.LifestyleOptionId);

            int matchedAnswersCount = optionsA.Intersect(optionsB).Count();

            //Tính toán phần trăm (Chỉ 1 phép tính đơn giản)
            int score = (int)Math.Round((double)matchedAnswersCount / totalQuestions * 100);

            return new MatchingResultDTO
            {
                TargetUserId = targetUserId,
                MatchingScore = score,
                TotalQuestions = totalQuestions,
                MatchedAnswers = matchedAnswersCount
            };
        }
        //gợi í danh sach những người phù hợp nhất dựa trên câu trả lời của họ (để gợi ý phòng ở phù hợp)
        public async Task<List<SwipeCardDTO>> GetSwipeDeckAsync(string currentUserId, int limit)
        {
            // 1. Lấy đáp án của current user
            var currentUserAnswers = await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId == currentUserId);

            var myOptionIds = currentUserAnswers.Select(x => x.LifestyleOptionId).ToList();
            int totalQuestions = myOptionIds.Count;

            // Nếu user chưa làm bài test, trả về list rỗng
            if (totalQuestions == 0) return new List<SwipeCardDTO>();

            // 2. Lấy danh sách ID những người user này ĐÃ QUẸT
            var swipedHistory = await _unitOfWork.Repository<UserSwipe>()
                .FindAsync(s => s.SwiperId == currentUserId);
            var swipedIds = swipedHistory.Select(s => s.SwipedUserId).ToList();

            // 3. Lấy đáp án của TẤT CẢ những người khác (Trừ bản thân và những người đã quẹt)
            var allOtherAnswers = await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId != currentUserId && !swipedIds.Contains(u.UserId));

            // 4. Gom nhóm đáp án theo từng UserId
            var groupedAnswers = allOtherAnswers.GroupBy(u => u.UserId);

            var deck = new List<SwipeCardDTO>();

            // 5. Tính điểm hàng loạt
            foreach (var group in groupedAnswers)
            {
                var theirOptionIds = group.Select(x => x.LifestyleOptionId).ToList();

                // Dùng phép giao (Intersect) để đếm số đáp án giống nhau
                int matchedCount = myOptionIds.Intersect(theirOptionIds).Count();

                int score = (int)Math.Round((double)matchedCount / totalQuestions * 100);

                deck.Add(new SwipeCardDTO
                {
                    UserId = group.Key,
                    MatchingScore = score
                });
            }

            // 6. Xào bài: Lấy những người hợp trên 50%, trộn ngẫu nhiên (hoặc sắp xếp cao xuống thấp), rồi cắt đúng 10 thẻ
            return deck
                .Where(d => d.MatchingScore >= 50) // Có thể bỏ dòng này nếu muốn hiện cả người không hợp
                .OrderByDescending(d => d.MatchingScore) // Ưu tiên người hợp nhất đưa lên trên
                                                         // .OrderBy(x => Guid.NewGuid()) // Nếu muốn xào trộn ngẫu nhiên thì mở comment dòng này
                .Take(limit)
                .ToList();
        }

        public async Task<List<UserLifestyleAnswerDTO>> GetUserAnswersAsync(string userId)
        {
            var userAnswers = (await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId == userId)).ToList();

            if (!userAnswers.Any())
                return new List<UserLifestyleAnswerDTO>();

            var optionIds = userAnswers.Select(a => a.LifestyleOptionId).Distinct().ToList();
            var options = (await _unitOfWork.Repository<LifestyleOption>()
                .FindAsync(o => optionIds.Contains(o.Id))).ToList();

            var questionIds = options.Select(o => o.LifestyleQuestionId).Distinct().ToList();
            var questions = (await _unitOfWork.Repository<LifestyleQuestion>()
                .FindAsync(q => questionIds.Contains(q.Id))).ToList();

            var seenQuestions = new HashSet<int>();
            var result = new List<UserLifestyleAnswerDTO>();

            foreach (var answer in userAnswers.OrderBy(a => a.LifestyleQuestionId))
            {
                var opt = options.FirstOrDefault(o => o.Id == answer.LifestyleOptionId);
                if (opt == null) continue;
                if (!seenQuestions.Add(opt.LifestyleQuestionId)) continue;

                var q = questions.FirstOrDefault(x => x.Id == opt.LifestyleQuestionId);
                result.Add(new UserLifestyleAnswerDTO
                {
                    QuestionId = opt.LifestyleQuestionId,
                    QuestionContent = q?.Content ?? string.Empty,
                    OptionId = opt.Id,
                    OptionContent = opt.Content
                });
            }

            return result.OrderBy(r => r.QuestionId).ToList();
        }

        public async Task SaveSwipeActionAsync(string currentUserId, string targetUserId, bool isLike)
        {
            var newSwipe = new UserSwipe
            {
                SwiperId = currentUserId,
                SwipedUserId = targetUserId,
                IsLike = isLike
            };

            await _unitOfWork.Repository<UserSwipe>().AddAsync(newSwipe);
            await _unitOfWork.CompleteAsync();
        }
        public async Task<LifestyleQuestion> UpdateQuestionOnlyAsync(UpdateQuestionDTO dto)
        {
            var question = await _unitOfWork.Repository<LifestyleQuestion>().GetByIdAsync(dto.Id);
            if (question == null) throw new KeyNotFoundException("Câu hỏi không tồn tại.");

            // Chỉ cập nhật nội dung câu hỏi
            question.Content = dto.Content;

            await _unitOfWork.CompleteAsync();

            return question;
        }
        public async Task UpdateQuestionOptionsAsync(int questionId, List<UpdateOptionDTO> incomingOptions)
        {
            // Kiểm tra xem câu hỏi có tồn tại không (Tùy chọn, để đảm bảo data chuẩn)
            var questionExists = await _unitOfWork.Repository<LifestyleQuestion>().GetByIdAsync(questionId) != null;
            if (!questionExists) throw new KeyNotFoundException("Câu hỏi không tồn tại.");

            // Lấy các câu trả lời đang có sẵn của câu hỏi này trong DB
            var existingOptionsList = (await _unitOfWork.Repository<LifestyleOption>()
                .FindAsync(o => o.LifestyleQuestionId == questionId)).ToList();

            // Duyệt qua list câu trả lời Frontend gửi lên
            foreach (var incomingOption in incomingOptions)
            {
                // TRƯỜNG HỢP A: Đổi nội dung câu trả lời cũ (Có Id)
                if (incomingOption.OptionId.HasValue && incomingOption.OptionId.Value > 0)
                {
                    var existing = existingOptionsList.FirstOrDefault(o => o.Id == incomingOption.OptionId.Value);
                    if (existing != null)
                    {
                        existing.Content = incomingOption.Content; // Ghi đè nội dung mới
                    }
                }
                // TRƯỜNG HỢP B: Thêm câu trả lời mới (Không có Id)
                else
                {
                    var newOption = new LifestyleOption
                    {
                        Content = incomingOption.Content,
                        LifestyleQuestionId = questionId
                    };

                    // Giả sử Repository của bạn có hàm Add()
                    _unitOfWork.Repository<LifestyleOption>().AddAsync(newOption);
                }
            }

            // Lưu toàn bộ thay đổi của Options xuống Database
            await _unitOfWork.CompleteAsync();
        }

        private static DateTime GetStartOfWeekUtc(DateTime utcNow)
        {
            var date = utcNow.Date;
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff);
        }

        public async Task<List<WishlistItemDTO>> GetMyLikesAsync(string currentUserId)
        {
            var currentUserAnswers = await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => u.UserId == currentUserId);
            var myOptionIds = currentUserAnswers.Select(x => x.LifestyleOptionId).ToList();

            var likes = (await _unitOfWork.Repository<UserSwipe>()
                .FindAsync(s => s.SwiperId == currentUserId && s.IsLike))
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            if (!likes.Any()) return new List<WishlistItemDTO>();

            var targetUserIds = likes.Select(x => x.SwipedUserId).Distinct().ToList();
            var accounts = (await _unitOfWork.Repository<Account>()
                .FindAsync(a => targetUserIds.Contains(a.Id.ToString())))
                .ToList();

            var targetAnswers = (await _unitOfWork.Repository<UserLifestyle>()
                .FindAsync(u => targetUserIds.Contains(u.UserId)))
                .ToList();

            var result = likes.Select(like =>
            {
                var account = accounts.FirstOrDefault(a => a.Id.ToString() == like.SwipedUserId);
                var theirOptionIds = targetAnswers
                    .Where(a => a.UserId == like.SwipedUserId)
                    .Select(a => a.LifestyleOptionId)
                    .ToList();

                var matched = myOptionIds.Intersect(theirOptionIds).Count();
                var score = myOptionIds.Count == 0 ? 0 : (int)Math.Round((double)matched / myOptionIds.Count * 100);
                var displayName = account == null
                    ? like.SwipedUserId
                    : string.Join(" ", new[] { account.FirstName, account.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

                return new WishlistItemDTO
                {
                    UserId = like.SwipedUserId,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? like.SwipedUserId : displayName,
                    AvatarUrl = account?.ProfileImages?.FirstOrDefault(),
                    MatchingScore = score,
                    LikedAt = like.CreatedAt
                };
            });

            return result.ToList();
        }

        public async Task<bool> RemoveLikeAsync(string currentUserId, string targetUserId)
        {
            var existing = (await _unitOfWork.Repository<UserSwipe>()
                .FindAsync(s => s.SwiperId == currentUserId && s.SwipedUserId == targetUserId && s.IsLike))
                .FirstOrDefault();

            if (existing == null) return false;

            _unitOfWork.Repository<UserSwipe>().Remove(existing);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<SwipeQuotaDTO> GetSwipeQuotaAsync(string currentUserId)
        {
            var now = DateTime.UtcNow;
            var startOfWeek = GetStartOfWeekUtc(now);
            var nextWeekStart = startOfWeek.AddDays(7);

            // TODO: thay bằng logic premium thực tế khi có bảng gói của user
            var isPremium = false;
            var weeklyLimit = isPremium ? (int?)null : 10;

            var usedThisWeek = (await _unitOfWork.Repository<UserSwipe>()
                .FindAsync(s => s.SwiperId == currentUserId && s.CreatedAt >= startOfWeek)).Count();

            return new SwipeQuotaDTO
            {
                IsPremium = isPremium,
                WeeklyLimit = weeklyLimit,
                UsedThisWeek = usedThisWeek,
                Remaining = isPremium ? null : Math.Max(weeklyLimit.Value - usedThisWeek, 0),
                WeekResetAt = nextWeekStart
            };
        }
    }
}
