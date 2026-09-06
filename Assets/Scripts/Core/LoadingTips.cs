using UnityEngine;

// Short pieces of advice shown while a level loads.
public static class LoadingTips
{
    private static readonly string[] Tips =
    {
        "Dash tiêu tốn 1 stamina.",
        "Tiêu diệt toàn bộ quái để mở cổng.",
        "Thu thập vàng để mua vũ khí trong Cửa hàng.",
        "Bow giúp tấn công kẻ địch từ xa.",
        "Staff gây sát thương mạnh ở khoảng cách xa.",
        "Stamina tự hồi lại sau một khoảng thời gian.",
        "Quái đã bị tiêu diệt sẽ không hồi sinh khi bạn quay lại.",
        "Nếu Health về 0, toàn bộ lượt chơi sẽ kết thúc.",
        "Bạn có thể quay lại màn trước bất cứ lúc nào.",
        "Nhớ Lưu Game trong menu Tạm dừng trước khi thoát."
    };

    public static string Random()
    {
        return Tips[UnityEngine.Random.Range(0, Tips.Length)];
    }
}
