# Đối chiếu demo Home và gameplay — 23/10/2026

Chuẩn đối chiếu là demo rút gọn do chủ dự án cung cấp, không bổ sung hệ thống từ bản Cocos/store chỉ vì bản gốc có nó.

## Những điểm xác nhận và đã sửa

| Hạng mục | Trước khi kiểm tra | Sau khi sửa |
|---|---|---|
| Ốc trên Home | Bị dùng làm tiến độ level `n / 89` | Là tiền tệ; số dư độc lập level, lưu trong `ScrewKing.Currency` |
| Số 9099 trong ảnh | Không có ví thật | Là số dư minh họa; người chơi mới bắt đầu 0 |
| Thưởng thắng | Không có | +20 mỗi lượt thắng, chỉ cộng một lần; thua/retry/về Home không tự cộng |
| Chọn level Home | Hai mũi tên thêm ngoài demo | Đã ẩn trong prefab; giữ nút Level để vào chơi |
| Chữ MAIN LEVEL | Thêm ngoài demo | Đã ẩn |
| Gameplay HOME | Thêm ngoài demo | Đã ẩn; Quit nằm trong Settings |
| Tiến độ và số hộp | Thêm ngoài demo | Đã ẩn trong prefab |
| Hướng dẫn chữ thường trực | Thêm ngoài demo | Mặc định tắt; gợi ý dùng búa khi đang chọn mục tiêu vẫn giữ để thao tác rõ ràng |
| Tên booster | + SLOT / CLEAR / HAMMER | ADD SLOT / CLEAR SLOT / GLASS HAMMER |

UI vẫn là uGUI/prefab; không dựng lại toàn bộ prefab và không thay vị trí board/ốc, luật chơi hay dữ liệu level trong lần sửa này. Ốc màu trong bàn chơi vẫn là vật thể gameplay, tách biệt với đơn vị tiền trong ví.

## Điểm còn lệch hoặc chưa có chức năng

- Home: Shop/Cards mới là hình trang trí có chữ Soon và bị làm mờ. Demo vẽ hai icon đầy đủ nhưng không cung cấp màn Shop/Cards hay quy tắc tiêu tiền. Chưa tự tạo cửa hàng, bộ sưu tập hoặc cách tiêu tiền.
- Màn thắng (đã bổ sung theo lựa chọn giữ x2): ví ở góc trên, nhãn +20, nút video + ốc x2 và Next level phía dưới theo demo. Hoàn tất video được thêm 20 (tổng 40); đổi nhãn +40 và khoá x2. Huỷ/lỗi không cộng, có thể thử lại khi quảng cáo sẵn sàng. Callback trùng hoặc từ lượt chơi cũ bị bỏ qua.
- Màn thua (đã bổ sung): ví ở góc trên và hình 20 gạch chéo, ẩn nút đóng ngoài demo. Hiện không trừ tiền khi thua.
- Header gameplay: nền nhãn level hiện là hình phẳng, chưa giống hoàn toàn viên nâu bo tròn trong demo. Tỷ lệ/spacing hộp và cụm booster còn cần một lượt căn art ở cùng tỷ lệ màn hình; không khẳng định pixel-perfect.
- Popup xác nhận xem quảng cáo là phần thêm so với các ảnh demo được cung cấp. Chưa có demo riêng cho popup này.
- Hàng 11 màu ốc ở giữa ảnh `SCREW copy.jpg` là hình trình bày bộ art; không coi đó là một thanh UI cần thêm vào gameplay.
- Nút x2 đã nối Ads/Tracking SDK base qua placement `level_reward_double`; chỉ `AdResult.Completed` cấp thêm tiền. Quảng cáo thật chưa xác minh trên thiết bị TikTok vì chưa có App ID/ad unit. Nguồn SDK base giữ nguyên. Bản Unity native không có SDK quảng cáo, dùng mô phỏng và có nhãn Local preview dưới nút.

## Kiểm chứng

Unity đã biên dịch và chạy Play Mode trên project TikTok: Home hiển thị số dư, không hiện mũi tên chọn level; level 1 thắng thật, lưu +20 đúng một lần, sang level 2, huỷ yêu cầu booster, mô phỏng trạng thái thua để kiểm tra popup/retry, rồi Quit về Home. Đồng thời kiểm tra setting/pause, tham chiếu prefab, đường bay đến slot và giữ nguyên chỉnh sửa layout/text thủ công. Kết quả: PASS trong `Builds/demo-audit-play.log` và `Builds/NewArtUI/prefab-play-smoke.json`.

Các thay đổi tương ứng đã áp dụng vào cả Unity và TikTok; lượt Play Mode kiểm chứng chạy ở TikTok. Bổ sung x2 ngày 23/10: kiểm tra trạng thái completed/cancel/failed/duplicate/overlap/stale/reset và giả lập kết quả SDK tại callback của game, xác minh lưu ví +40, cập nhật popup và khoá nút. Kết quả PASS trong `Builds/double-reward-play.log`. Không coi mô phỏng là kiểm chứng quảng cáo TikTok thật.

WebGL build sau khi bổ sung x2: Succeeded, 0 lỗi. Chơi thắng level 1 thật trong trình duyệt, ví hiện 20, nhãn +20, nút video/ốc/x2 và Next level đúng thứ tự; không có lỗi console. Ảnh kiểm chứng `Builds/DoubleReward/win-x2.jpg`. Nút quảng cáo bị vô hiệu khi provider chưa sẵn sàng; chưa có quảng cáo TikTok thật để chạy trên thiết bị.
