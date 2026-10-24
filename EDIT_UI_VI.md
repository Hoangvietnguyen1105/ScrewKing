# Chỉnh UI bằng prefab

Mở scene `Assets/ScrewKing/Scenes/UIEditing.unity`, hoặc dùng menu **Screw King > UI > Open UI Editing Scene**.

Các prefab nằm trong `Assets/ScrewKing/Resources/UI/Prefabs/`:

- **ScreenHome**: màn Home, nút Play, chọn level, Settings và thanh dưới.
- **ScreenLoading**: hai cánh cửa loading.
- **PopupSettings**: Music, Sound, Vibration, Resume, Quit.
- **PopupWin**, **PopupLose**: kết quả, Next level, Retry.
- **PopupReward**: đề nghị booster/hồi sinh, Watch và Cancel.
- **GameplayHUD**: lớp HUD gameplay, nằm dưới các popup.
- **ScrewKingUI**: Canvas tổng, chứa các prefab trên dưới DesignArea.

Double-click prefab cần sửa. Chọn object con trong Hierarchy để chỉnh **Rect Transform**, thay **Image > Source Image**, sửa **Text**, font, màu và Outline. Lưu prefab bằng Ctrl/Cmd+S. Để sửa riêng trong scene, dùng Prefab Overrides > Apply nếu muốn lưu về asset dùng trong game.

Các nút đã có On Click nối sẵn tới ScrewKingUiAction.Invoke. Trường Command trên cùng object xác định hành động. Có thể đổi hình, vị trí và chữ tĩnh mà không cần sửa code hoặc nối lại nút.

Chỉ nội dung phụ thuộc ván chơi được cập nhật lúc chạy: số level, trạng thái quảng cáo, tiêu đề đề nghị và nhãn bật/tắt. Vị trí các phần tử không bị đặt lại bằng code. Hai cánh cửa và Content của popup kết quả có chuyển động mở/xuất hiện; chỉnh thời lượng trong ScrewKingUiView.

Trong scene UIEditing, chọn ScrewKingUI rồi mở menu ngữ cảnh của component ScrewKingUiController > Preview để xem từng popup ở Edit mode. Hoặc mở riêng prefab popup. Khi Play, game tự chuyển màn theo trạng thái.

Build và Play dùng chính các prefab này. Menu **Create Missing Prefabs** chỉ tạo asset còn thiếu, không ghi đè các prefab đã chỉnh. Không cần chạy lại menu này khi thay art/layout.

Bản desktop và TikTok có cùng cấu trúc prefab nhưng là hai project độc lập: sửa trong project đang dùng; nếu cần cả hai, chép prefab/art liên quan cùng file .meta sang project kia. HUD trên bàn chơi đã được đưa vào GameplayHUD dưới cùng Canvas để không đè popup. Các phần động của HUD vẫn lấy vị trí từ model; Home, Loading và các popup là các prefab chỉnh sửa riêng.

## HUD gameplay đã có sẵn trong prefab

Mở **GameplayHUD.prefab** để chỉnh Header, Progress, Boxes, BufferSlots, Boosters, Hint, Message và Transition. Không cần Play để thấy bố cục. Trong scene UIEditing, chọn ScrewKingUI > component ScrewKingUiController > Preview > Gameplay HUD.

Thư mục **Prefabs/Widgets** có HudBox, HudClosingBox, HudBufferSlot, HudFlyingScrew, HudGuide và ba prefab booster Slot/Clear/Hammer. Các instance hộp, 16 ô chứa và ba booster đã nằm sẵn trong GameplayHUD.

- Đổi vị trí/kích thước ở root hoặc object hình/chữ. Các node có tên **Motion / animated / from model** là phần game điều khiển để chạy hiệu ứng. Để giữ thay đổi layout, chỉnh root hoặc các object hình/chữ bên trong thay vì dùng Motion làm bố cục tĩnh.
- Text tĩnh như nhãn booster và hướng dẫn không bị code đặt lại. Level/BoxCounter là nội dung động; sửa LevelFormat/CounterFormat trên component ScrewKingGameplayHud nếu muốn đổi cách ghi.
- Hình các màu hộp/ốc/nắp được chọn theo dữ liệu ván chơi. Đổi art theo màu trong **Assets/ScrewKing/Resources/UI/HudPalette.asset**. Các hình/nút tĩnh chỉnh trực tiếp trong Image.
- Các ô chứa tự dàn lại theo sức chứa 5–16 của game. Root của từng ô vẫn giữ offset bạn chỉnh; chỉ Motion bên trong nhận phần dịch chuyển từ model.
- Các node ốc bay, nắp và hướng dẫn có script chạy chuyển động. Game tạo bản sao từ prefab khi cần; không tạo Image/Text/Button bằng code. Đích của ốc bay đọc từ vị trí slot trong prefab để bám đúng khi bạn chỉnh layout.

Công cụ tạo UI chỉ khởi tạo mặc định hoặc nâng cấp HUD cũ còn trống. Những prefab đã có bố cục không bị dựng lại khi Play/build. Các prefab Home/popup hiện tại được giữ nguyên khi nâng cấp HUD.

## Hiệu ứng button

Các Button trong prefab có component **ScrewKingButtonFeedback**. Nhấn thu về 94%, thả nảy nhẹ trong 0.24 giây; hover chuột phóng nhẹ 1.8%. Chỉnh Press Scale, Hover Scale, Rebound, Release Duration trong Inspector. Visual chọn phần hình chuyển động (mặc định là nút); khi ẩn/khóa nút, hiệu ứng tự trả về scale/vị trí gốc. Dùng thời gian unscaled nên popup Settings vẫn có phản hồi khi gameplay pause. Không thêm tiếng hoặc rung riêng cho nút.

## Cụm booster theo demo art (24/10/2026)

Chỉnh riêng các prefab Widgets/HudBoosterSlot, HudBoosterClear, HudBoosterHammer. Icon giữ đúng tỷ lệ PNG gốc, có tính phần lề trong suốt; đế 134 x 134, nhãn 136.11 x 51.39 trên hệ 750 x 1334. Font caption dùng Lilita One (kèm OFL.txt trong UI/Fonts), GLASS HAMMER xuống đúng 2 dòng. Row Boosters trong GameplayHUD đặt y=1163.8, giữ khoảng trống cuối màn như demo. Các callback và hiệu ứng nhấn giữ nguyên.

## Popup Revive / booster mới (24/10/2026)

PopupReward dùng khung vàng, header có ốc vít, font Lilita One và icon đúng với hành động (hộp / thêm slot / chổi / búa). Chỉnh Panel, Header, RewardMedallion, ScrewIllustration, Description, WatchButton, Status và CancelButton trong Content. Status là ghi chú nhỏ; bản native ghi Preview · reward simulated và nút Continue, bản TikTok dùng Watch video. Cancel ở Revive ghi Give up vì sẽ kết thúc lượt. Popup có hiệu ứng mở nhẹ và giữ callback cũ.
