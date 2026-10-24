# Screw King Unity — source simulation 0.2

> Cập nhật dữ liệu màn: bản mới dùng `res/config/levelCom` và `res/config/level/<id>` nhúng trong APK. Bộ `recovered/level_data/normal/level` đã dùng trước đây là bộ khác, không phải nguồn mà nhánh local của `GameCtrl.preloadLevel` nạp. Bản chơi mới: `../Builds/Screw King Bundled Levels.app`. Các app cũ trong Builds vẫn dùng bộ màn trước khi sửa.

Project: Unity 6000.3.21f1. Mở scene Assets/ScrewKing/Scenes/ScrewKing.unity. Bản mới: ../Builds/Screw King Unity Source.app. Bản 0.1 cũ vẫn ở ../Builds/Screw King Unity.app để đối chiếu; không dùng 0.1 để đánh giá độ khớp logic.

## Thay đổi quan trọng
Bản này KHÔNG thay solver gốc bằng Physics2D của Unity. Unity hiển thị, nhận thao tác và chạy state machine C#; JavaScriptCore native macOS thực thi chính code sinh màn và bộ Box2D JavaScript được đóng gói trong APK. Không sử dụng WebView hoặc mở game HTML trong cửa sổ Unity. Backend này hiện dành cho macOS; muốn build Android/Windows cần backend JavaScript tương ứng.

CocosPhysics.js.txt giữ nguyên toàn bộ engine, chỉ thay bootstrap để lấy module mà không khởi động game/browser. CocosRules.js.txt giữ nguyên thân 6 module: GameUI, MainConst, Enums, ArrayUtils, ComBox, ComHole. GameUI.updateCfg thực sự chạy để tạo màu ốc và hàng đợi hộp. Đã bỏ Fisher–Yates và remap màu tự thêm của bản 0.1. MainConst có updateTypes nhưng không thấy call site trong bundle active, nên giữ ánh xạ mặc định theo nhánh đang chạy.

89 layout và 108 polygon vẫn là dữ liệu gốc. Box2D gốc thực hiện cả chia polygon lồi, fixture, khớp, trọng lực, giảm tốc và bước giải. Physics2D/Collider2D của Unity không tham gia quyết định xoay/rơi/che khuất. Che ốc dùng chính cc.Intersection.polygonCircle với radius=25px; vùng bấm theo prefab 75x75. Kích thước, khoảng cách ô chờ và vị trí chơi theo prefab ở viewport 750x1334.

## State machine
C# tách reservation khỏi arrival: hộp/ô chờ được giữ chỗ ngay; kết thúc hộp chỉ sau khi các ốc hạ xong. Nhấc .15s + chờ .05s; bay 3000px/s theo vị trí hộp đang chuyển động; đặt .15s + chờ .05s. Hộp tiếp theo vào sau .4s, hộp cũ dọn xong sau .6s. Mở lớp dưới sau .5s và giữ khoảng .4s reveal trước khi nhận click ở lớp mới.

Hai hộp khóa, hai hộp mở và năm ô chờ như config gốc. Đầy ô chỉ xét khi ốc đã ngồi yên, không còn thao tác/đóng hộp; nhánh offer clear/revive được tách khỏi thất bại. Clear chỉ đưa ốc vào cache, không tự lấp đầy mọi hộp. Swap và hammer áp dụng điều kiện cache/hàng đợi theo code nguồn. Thêm ô tối đa 5+11=16.

## Reward trong bản local
Booster và mở hộp mở dialog, không còn cấp trực tiếp bằng một lần bấm. Bản APK đòi rewarded-video success; bản local không kết nối mạng quảng cáo và cho hai nút kiểm thử kết quả success/cancel. Đây là mô phỏng callback, không phải tích hợp quảng cáo thật và không thể xem là tương đương trải nghiệm monetization hoàn chỉnh.

## Kiểm chứng
- SourceAssetIntegrity.json: 6 thân module khớp nguồn; engine chỉ đổi bootstrap.
- SourceParityVerification.json: 89 màn x 2 seed; 2.244 thao tác prefix bảo toàn ốc; 6 ca routing gọi từ source thật; các kiểm tra timing và clear/cache.
- Mô phỏng 1.572 ván ở 89 màn, 20 bước mỗi màn: không NaN/Infinity.
- SourceOracle.json: trace xoay/rơi từ đúng engine chạy độc lập trong Node/V8. Backend JavaScriptCore cho sai lệch tối đa khoảng 2.3e-13px và 1.5e-13 độ trên 5 mốc; trigger dọn ván cùng bước 157.
- SourceVisualSmoke/runtime-smoke.json: ứng dụng macOS đã đóng gói chạy 6 thao tác hợp lệ, giữ 15/15 ốc của màn 1. Ảnh trước/sau ở cùng thư mục. Không có exception trong log kiểm thử.

Các kiểm tra này không phải bằng chứng mọi trình tự thao tác trên mọi thiết bị khớp 100%. Game source dùng random.sort với comparator không nhất quán; cùng code có thể cho trình tự khác giữa các engine JS. Backend macOS dùng JavaScriptCore, không tuyên bố tái tạo chuỗi random của một phiên Android/V8 cụ thể. Test dùng seed/tape kiểm soát để có thể chạy lại; phiên gốc không cung cấp seed đã capture.

Chưa có replay đầu-cuối trên chính APK/thiết bị cho toàn bộ 89 màn; UI và hiệu ứng chưa khôi phục từng pixel. State machine thời gian vẫn là bản C# đối chiếu nguồn, không phải toàn bộ scheduler Cocos. Không port meta, mini game, Spine UI hoặc dịch vụ live. Vì vậy chưa gắn nhãn toàn bộ sản phẩm là bản sao hoàn chỉnh 100%.

## Mã nguồn
- Scripts/CocosSourceRuntime.cs: bridge native JavaScriptCore.
- Resources/Source/: code nguồn và adapter không phụ thuộc rendering.
- Scripts/PortData.cs: state machine C# theo các callback nguồn.
- Scripts/BoardActor.cs: renderer ván và điều khiển source physics.
- Scripts/PortGame.cs: input, UI, audio và smoke test.
- Editor/PortBuilder.cs: importer, kiểm tra và build macOS.

Tạo lại source resources bằng reverse/tools/build_source_runtime.cjs; giữ Bridge.js.txt (adapter riêng) không bị ghi đè. Chạy reverse/tools/generate_source_oracle.cjs để tạo fixture; reverse/tools/verify_source_assets.py để kiểm tra các thân module. Entry point build: ScrewKing.Editor.PortBuilder.BuildMac.


## Reskin NewArt — 22/10/2026

Art mới được dùng trực tiếp từ `Resources/NewArt`; art gốc được giữ lại cho các ván và phần chưa có asset thay thế.

- Nền gỗ, ốc, hộp, nắp hộp hoàn tất, hộp khóa, ô chờ, pause, nút xanh và icon booster đã dùng bộ mới.
- Màu gameplay vẫn từ 1–11. `ReskinArt.cs` ghép màu ốc theo màu hộp: d1, d2, d3, d4, d5, d6, d11, d10, d9, d8, d7. Tên file do bộ art đặt chưa cùng thứ tự giữa ốc và hộp.
- Đầu ốc trong màn chơi được chuẩn hóa về 54px; vị trí bay, vùng bấm, physics và luật màn không thay đổi.
- Bộ art chưa có ván mới hoặc icon Swap mới nên hai phần này tiếp tục dùng art có sẵn.
- Import PNG mới thành single sprite, 100 pixels/unit, không mipmap, không nén, giữ kích thước gốc và alpha.

Bản chơi macOS: `../Builds/Screw King Reskin.app`. Ảnh và kết quả chạy thử: `../Builds/ReskinVisualSmoke/`. Chạy `Screw King/Validate new art` trong Unity để kiểm tra asset; `PortBuilder.BuildMac` xuất bản reskin và chạy kiểm tra logic 89 màn trước khi build.


## Khôi phục bản reskin

Đã rút lại đợt hiệu ứng gameplay sau phản hồi lỗi hiển thị. Mã runtime được khôi phục về bản reskin trước khi thêm hiệu ứng. Bản phát hành sử dụng: `../Builds/Screw King Reskin.app`. Bản hiệu ứng thử nghiệm và bằng chứng kiểm tra cũ được lưu trong `../Builds/WithdrawnEffects/`, không dùng làm bản đã duyệt. Kiểm tra logic 89 màn trước đó không xác nhận chất lượng chuyển động.


## Bay ốc đơn giản bằng LitMotion

Theo yêu cầu sau phản hồi: chỉ dùng một tween LitMotion từ vị trí ốc tới đúng ô đích, .35s Ease.InOutSine. Không xoay, đổi kích thước, rung, offset phụ, nhún hộp hay đổi reveal của ván. Vị trí đích vẫn lấy từ model; mỗi ốc chỉ có một tween, tween cũ bị hủy khi dùng lại hoặc đổi màn. Ốc đang bay được vẽ một lần phía trên hộp; đầu ốc giữ 54px khi bay và khi ngồi trong hộp/ô chờ. Callback gameplay vẫn giữ theo model cũ.

LitMotion 2.0.2 được nhúng tại `Packages/com.annulusgames.lit-motion`, từ kho chính thức `annulusgames/LitMotion`, commit `422eb124051c81a9bc3f422ebf190703c5053514`, kèm giấy phép MIT. Dependency Burst/Collections/Mathematics được khai báo trong manifest.

Bản chơi mới: `../Builds/Screw King Simple Flight.app`. Bản reskin trước hiệu ứng vẫn ở `../Builds/Screw King Reskin.app`. Kiểm tra tween không đảo hướng/vượt đích, hủy tween khi dùng lại và khi reset: `Screw King/Validate simple LitMotion flight`.


## Sửa nguồn dữ liệu màn theo đối chiếu mobile level 2

`GameCtrl.getLevelRes` chọn ID theo `config/levelCom`; `preloadLevel` khi jsonUrl không chứa http nạp `config/level/<id>` từ bundle res. Port trước đây đã bỏ qua nhánh này và dùng bộ JSON khác ở `recovered/level_data/normal/level`.

- Đã xuất lại đủ 89 màn theo danh sách `config/levelCom`, giữ nguyên tọa độ, hình ván, góc xoay, vị trí ốc và thứ tự lớp của asset nhúng.
- Level 1 mới có 9 ốc, 2 lớp: hai ván bốn cánh và một mặt cười. Level 2 có 120 ốc, 12 lớp; lớp trên cùng là mặt cười 86, ngay dưới là bốn ván bốn cánh 28.
- Với trường show/gray không có trong asset, dùng mặc định GameServerConf trong source: 5 lớp hiện, 2 lớp xám. Các giá trị có sẵn trong từng màn được giữ nguyên.
- `reverse/tools/export_bundled_levels.py` xuất dữ liệu; `reverse/recovered/bundled_level_manifest.json` lưu ID, đường dẫn nguồn và SHA256. Bộ cũ được lưu ở `reverse/recovered/unity_previous_normal_levels` để đối chiếu.
- Fixture kiểm chứng physics cũ được giữ riêng ở `Data/PhysicsOracleLevel.json`; không dùng làm màn chơi. Kiểm tra V8/JavaScriptCore của fixture vẫn giữ độc lập với việc chọn corpus màn.

Bố cục màn đã được sửa theo nguồn và đối chiếu hình dạng trong ảnh người dùng. Không suy ra toàn bộ màu ngẫu nhiên hoặc lớp bị che từ một ảnh. UI reskin, vị trí vùng chơi trên các tỷ lệ màn hình và màu ván hiện chưa được xác nhận khớp từng pixel với mobile.


## Sửa lỗi không vào được level trong Editor

Log Editor xác nhận `get_level is not allowed ... MonoBehaviour constructor` phát sinh từ `PortGame` khởi tạo `SimpleScrewFlight` trong field initializer; LitMotion/Burst cấp phát bộ nhớ tại thời điểm Unity deserialize scene. Đã chuyển khởi tạo sang Awake, bảo vệ OnDestroy khi chưa khởi tạo và ngăn Update/OnGUI truy cập runtime chưa sẵn sàng.

Đã kiểm tra bằng Play mode thật trong Editor: load scene khi đang Edit, tạo/hủy component chưa Awake, vào level 2, tháo một ốc, đổi sang level 1 rồi Stop/Play lại; hai vòng không ghi nhận Error/Exception. Kết quả: `../Builds/EditorLifecycleVerification.json`. Entry point kiểm thử: `ScrewKing.Editor.EditorLifecycleSmoke.Run` (chạy bằng Unity batchmode, không thêm `-quit` vì cần qua các lần domain reload).

Sau khi sửa mã khi Unity đang Play, Stop, chờ Unity biên dịch xong rồi Play lại để tạo lại runtime đã khởi tạo lỗi trước đó.


## Main game effects — 22/10/2026

Bản mới: `../Builds/Screw King Main Effects.app`.

- Renderer đọc trực tiếp stage/position của PortModel: nâng và xoay 540° (.15s + .05s), bay theo đích đang chuyển động với 3000px/s, thu về .8, rồi hạ/vặn đặt (.15s + .05s). Không còn tween .35s độc lập trong PortGame. SimpleScrewFlight được giữ lại làm mã lịch sử, không điều khiển bản này.
- Hộp đầy: nắp hạ .2s, thân ép/hồi với BackOut, hộp đi lên .4s; full/exit/put phát theo sự kiện gameplay.
- Reveal ván .4s rồi fade ốc .4s; chỉ nhận click sau reveal. Có lỗ ốc theo ván và phản hồi rung ngang khi ốc bị che.
- Búa tạo mảnh ván với art đang dùng và dọn sau .75s; đây là hiệu ứng Unity dựng lại, không phải SpineBroke nguyên gốc. Overlay chọn ván, âm booster riêng, hướng dẫn màn đầu, panel kết quả hiện dần với scale và fade khi bắt đầu/chuyển màn.
- Rung thiết bị chỉ biên dịch cho iOS/Android; backend gameplay hiện vẫn là macOS như trước nên chưa kiểm chứng rung trên mobile.
- Không thêm confetti/glow/trail giả định. Panel kết quả và chuyển cảnh là bản Unity dựng lại; không tuyên bố pixel parity với toàn bộ RESULT Cocos.

Kiểm tra: `MainEffectsVerification.json` kiểm tra reservation, hình nhấc trước release, 3 callback đặt, bảo toàn ốc và thứ tự nắp/exit/dọn hộp. `MainEffectsSourceParityVerification.json` lưu kết quả kiểm tra 89 màn cho bản này. `MainEffectsVisualSmoke/effects-smoke.json` và ảnh đi kèm kiểm tra ứng dụng thật chơi hết màn 1 và thử búa/slot/cache/swap ở màn 2; các nhánh booster được gọi trực tiếp trong smoke, không thay cho kiểm thử reward UI đầy đủ.

Project kiểm thử tách ở `/tmp/ScrewKingEffectsValidation` vì Editor của người dùng đang mở project chính. BuildMac xuất app mới, giữ các app reskin/simple-flight cũ. Mã trước đợt này được sao lưu ở `../Builds/MainGameEffectsBackup/`.


## Căn vùng chơi theo widget Cocos — 23/10/2026

GameUI prefab có layerMain neo top=0/bottom=160, layerCen neo top=300/bottom=0. Với khung 750x1334, tâm vùng ván là `667 - 300 - (1334 - 160 - 300)/2 = -70`, không phải -230. Vùng ván đã được nâng 160px thiết kế. GameplayLayout chuyển tọa độ hiển thị và tọa độ bấm/che khuất ở cùng một ranh giới; Box2D vẫn giữ hệ tọa độ fixture/oracle cũ. Vị trí ốc bay xuất phát từ hình mới; hộp và ô chờ giữ nguyên vị trí. Đây là căn theo prefab gốc ở khung chuẩn, chưa khẳng định khớp mọi safe area của điện thoại.

Ảnh sau căn: `../Builds/LayoutVisualSmoke/Level1/initial.png` và `../Builds/LayoutVisualSmoke/Level2/unity-initial.png`. Báo cáo: `../Builds/GameplayLayoutVerification.json`. Video MainEffectsPreview.mp4 đã cập nhật theo vị trí mới.

## Thanh booster theo bản gốc — 23/10/2026

Đã ẩn Swap khỏi main game và căn giữa 3 nút theo thứ tự GameUI.init của Cocos: ADD_HOLE → CLEAR → HAMMER. Vùng chặn bấm xuyên xuống ván dùng cùng vị trí với các nút. Logic ChangeBox vẫn giữ trong model, không có nút kích hoạt trong HUD.
