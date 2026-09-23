// Everything the game says about its own world, in one place: the opening, the
// line that introduces each region, and the ending. Kept out of the screens
// that show it so the writing can be changed without touching any layout code.
public static class StoryContent
{
    public const string Title = "SOULBOUND GATE";

    // Shown one line at a time before the first level of a new run.
    public static readonly string[] Prologue =
    {
        "Vương quốc Aldmoor đã mất từ lâu. Thứ còn lại chỉ là một cánh cổng đá " +
        "giữa đồng cỏ, và ánh sáng xanh không bao giờ tắt bên trong nó.",

        "Người ta gọi đó là Cổng Linh Hồn. Mỗi người ngã xuống ở vùng đất này " +
        "đều bị cổng giữ lại, trở thành một ngọn đèn nhỏ trên vòm đá.",

        "Canh giữ cổng là Soul Warden — kẻ đã tự nguyện mang trên mình mọi linh " +
        "hồn nó bắt được, và vì thế không còn chết được nữa.",

        "Ngươi là người cuối cùng còn cầm được Thanh Kiếm Ràng Buộc. Đi qua năm " +
        "vùng đất, tới cánh cổng, và trả những linh hồn ấy về nơi họ thuộc về."
    };

    // Shown after the Soul Warden falls, before the victory screen.
    public static readonly string[] Epilogue =
    {
        "Soul Warden quỳ xuống. Lớp giáp linh hồn trên lưng nó vỡ ra thành hàng " +
        "trăm đốm sáng, bay ngược vào trong cổng.",

        "Từng ngọn đèn trên vòm đá tắt dần. Không phải vì lụi tàn — mà vì những " +
        "người bị giữ lại cuối cùng đã được đi tiếp.",

        "Cổng khép lại sau lưng ngươi, lần này là vĩnh viễn. Aldmoor vẫn mất, " +
        "nhưng nó đã thôi níu giữ người sống."
    };

    // One line per region, shown under the level name when it loads.
    public static string LoreFor(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1:
                return "Đồng cỏ của người sống ngày trước. Giờ chỉ còn nhớt xanh bò trên nền đất cũ.";

            case 2:
                return "Khu rừng nuốt ánh sáng. Thứ ném đá ra từ trong bóng tối đã không còn là người.";

            case 3:
                return "Ngã tư của những lời thì thầm. Đứng lâu ở đây sẽ nghe thấy tên chính mình.";

            case 4:
                return "Đầm lầy giữ lại những kẻ lạc đường. Chúng vẫn đang chờ người mới tới.";

            case 5:
                return "Cổng Linh Hồn. Soul Warden đứng đó, mang trên mình tất cả những ai nó đã giữ.";

            default:
                return "";
        }
    }

    // The two-sentence version, for the guide sheet in the menu.
    public const string Summary =
        "Cổng Linh Hồn giam giữ linh hồn của cả vương quốc Aldmoor, và Soul Warden canh giữ nó. " +
        "Bạn đi qua năm vùng đất để tới cổng và giải phóng những linh hồn bị giam.";
}
