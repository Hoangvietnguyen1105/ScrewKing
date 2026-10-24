# Tính năng nền tảng TikTok quốc tế — 24/10/2026

Đã gắn vào **ScrewKingTikTok**. Project ScrewKingUnity vẫn là bản tham chiếu desktop. Có code, prefab và kiểm thử; chưa có App ID/ad unit/backend HTTPS của chủ dự án, chưa xác minh trên TikTok thật và chưa upload.

## Đã có

| Phần | Cách hoạt động |
|---|---|
| Đăng nhập nền | TT.InitSDK → TT.Login → gửi code tạm tới backend → nhận định danh game đã xác minh. Không xin avatar/tên, không mở popup cấp quyền profile. |
| Home Screen | Nút Home và PopupTikTokShortcut; gọi TT.AddShortcut, sau đó kiểm tra TT.GetShortcutMissionReward. Chỉ canReceiveReward=true mới cho Claim. |
| Revisit | Nút Home và PopupTikTokRevisit; hướng dẫn Profile menu → TikTok Minis → mở game; TT.StartEntranceMission và TT.GetEntranceMissionReward. |
| Thưởng ốc | Mặc định shortcut +100 một lần/tài khoản trên bản lưu; revisit +50/ngày UTC. Chỉnh số lượng trong Settings.asset. Không thưởng vì chỉ bấm hướng dẫn. |
| Chống nhận trùng | Danh tính từ backend, lưu eligibility và receipt; ví thưởng và receipt nằm trong cùng một bản ghi. Lỗi ghi dữ liệu rollback, callback cũ/bị lặp bị bỏ qua. |
| Quay lại app | Nghe TTAppLifeCycle.OnShow và fallback OnApplicationPause; kiểm tra lại điều kiện, không tự cộng trùng. |
| Lưu native | Dùng TT.Save/TT.LoadSaving cho bản ghi nền tảng: tiến độ level, tiền nền, cài đặt, tiền thưởng và receipt. PlayerPrefs được giữ làm mirror để code gameplay/base hiện có tiếp tục hoạt động. |
| Tracking | Các event tiktok_growth_open/navigate/eligibility/claim/login đi qua Tracking service của SDK base. Không log code đăng nhập, token hay OpenID. |
| Pháp lý | Có trường TermsUrl/PrivacyUrl; phải nhập cùng URL vào Developer Portal để TikTok hiển thị trên loading. Không tự soạn/chấp thuận nội dung pháp lý thay chủ dự án. |
| Compatibility | Kiểm tra CanIUse cho từng cặp API. Tính năng không hỗ trợ được ẩn trên host; request lỗi/timeout có thể thử lại. |

Đây là bảo vệ nhận thưởng trong game và dữ liệu local của nền tảng, không phải hệ thống ví cloud/anti-cheat server đa thiết bị. Điều kiện hoàn thành nhiệm vụ luôn lấy từ TikTok. Không có IAP, bảng xếp hạng hay xin thông tin profile trong phạm vi này.

## Chỉnh giao diện

Các prefab uGUI nằm ở `Assets/ScrewKing/Resources/UI/Prefabs/`:

- `PopupTikTokShortcut.prefab`
- `PopupTikTokRevisit.prefab`
- `ScreenHome.prefab`: TikTokShortcutEntry, TikTokRevisitEntry.
- `ScrewKingUI.prefab`: hai popup và các tham chiếu được nối sẵn.

Sửa art, text hướng dẫn, RectTransform và hiệu ứng button trực tiếp trong prefab. Runtime chỉ cập nhật số thưởng, trạng thái, chữ nút và tính khả dụng. Đăng nhập nền không thêm popup consent.

Bản Editor/browser hiển thị UI xem thử theo `ShowPlatformUiInPreview`, nhưng không giả thành công API TikTok, không cấp thưởng nền tảng tự động. Các test host giả lập nằm riêng trong Editor và không vào player.

## Cấu hình cần điền

Mở `Assets/ScrewKing/Resources/TikTok/Settings.asset`:

1. **AppId**: ID công khai của game, khớp cấu hình cửa sổ TikTokGame/Build Minigame.
2. **LoginEndpoint**: HTTPS endpoint đầy đủ, ví dụ `https://<your-domain>/v1/tiktok/session`.
3. **TermsUrl / PrivacyUrl**: URL thật của chủ dự án. Cũng điền trong Basic Information của Developer Portal.
4. **ShortcutReward / RevisitReward**: mặc định 100 / 50 ốc.
5. **EnableGrowthFeatures**, **AutoGuideAfterTutorial**: bật mặc định. Gợi ý tự động chỉ xuất hiện một lần khi đã hoàn thành tutorial và trở về Home, sau loading, không chen vào bàn chơi.

Thêm domain backend vào danh sách domain được phép trên Portal. Các ID quảng cáo vẫn nằm ở `Assets/SDKSetting/SdkSettings.asset` theo SDK base.

**TTSDK hiện tại không hỗ trợ Application.OpenURL trong native container.** Vì vậy Terms/Privacy dùng trang loading của TikTok; liên kết trong Settings chỉ hiện ở Editor/browser khi URL hợp lệ. Không đặt một nút mở URL không hoạt động trong native game. Phải xác minh việc hiển thị pháp lý trên Portal/thiết bị thật.

## Backend đăng nhập

Thư mục `Backend/` có dịch vụ WSGI Python, SQLite và test, không cần thư viện bổ sung để chạy local:

```
cd Backend
python3 -m unittest -v
python3 server.py
```

`python3 server.py` phục vụ thử nghiệm ở 127.0.0.1:8781. Khi đưa lên máy chủ, dùng WSGI server và HTTPS reverse proxy; endpoint public phải có HTTPS. Chưa có deployment nào được thực hiện.

Điền biến môi trường ở máy chủ theo `Backend/.env.example`:

- `TIKTOK_CLIENT_KEY`
- `TIKTOK_CLIENT_SECRET`
- `SESSION_ID_SECRET`: chuỗi ngẫu nhiên tối thiểu 32 ký tự, giữ ổn định qua các lần deploy để định danh player không thay đổi.
- `DATABASE_PATH`: file SQLite cần lưu bền vững/backup.
- `ALLOWED_ORIGINS`: chỉ khai báo domain browser được phép nếu có; native request không có Origin không cần CORS.

Secret **không đặt trong Assets, Settings.asset hoặc gửi vào code client**. File .env không tự được đọc; cấu hình môi trường của tiến trình/hosting. Chỉ .env.example trống được đưa vào source.

Hợp đồng cho backend riêng:

```
POST /v1/tiktok/session
Content-Type: application/json
{"code":"<temporary code from TT.Login>"}

200 OK
{"playerId":"<stable opaque player id>","sessionToken":"<game session token>"}
```

Backend mẫu đổi code ở `https://open.tiktokapis.com/v2/oauth/token/`, giữ OpenID ở máy chủ, không trả access_token/refresh_token của TikTok về game, không xin profile. Code đã dùng bị chặn replay; session token được hash trong database và hết hạn sau 7 ngày. Có giới hạn request và CORS theo allowlist. Có GET /health và GET /v1/session để kiểm tra session. Endpoint chưa cấu hình secret trả 503, không giả đăng nhập thành công.

## Kiểm chứng và điều kiện phát hành

Các kết quả kiểm thử ở `../Builds/TikTok/`:

- `capability-ledger-tests.json`: eligibility, save rollback, claim trùng, restart, đổi ngày, account isolation, HTTPS, prefab wiring.
- `capability-play-tests.json`: game/host giả lập, login/session, popup, wallet, callback cũ, resume, preview guard.
- `sdk-tiktok-player-compile.json`: nhánh MB_TIKTOK biên dịch với DLL SDK thật.
- `preflight.json`: trạng thái cấu hình hiện tại.

`Backend/test_server.py` kiểm tra đăng nhập/session, code replay, expiry, config thiếu, origin, rate limit và restart database.

SDK MoodBoost/TTSDK/UniTask và VContainer vẫn giữ nguyên nguồn như project base. Tính năng mới nằm ở lớp game, không sửa vendor SDK.

Chưa thể đánh dấu đủ điều kiện upload:

- Thiếu App ID, ad unit, URL pháp lý thật, backend public và cấu hình Portal.
- Project đang Unity 6000.3.21f1; tài liệu TikTok hiện chỉ dẫn đóng gói bằng Unity 2021/2022. Cần một toolchain Unity được hỗ trợ và xử lý tương thích project trước khi xuất native package. Menu Build TikTok Package chặn điều kiện chưa đủ; không coi ZIP/WebGL preview là gói native.
- Cần kiểm chứng login, API support, shortcut, quay lại đúng entry, thưởng/lưu qua restart và đổi ngày, pause/resume, Terms/Privacy trên TikTok thật.
- Cần kiểm tra size gói native ≤60 MB, tốc độ tải, bộ nhớ và vùng an toàn trên thiết bị. Size bản browser không chứng minh size gói native.

Tài liệu đối chiếu:

- https://developers.tiktok.com/docs/en/tiktok-sdk-for-unity
- https://developers.tiktok.com/docs/en/silent-login
- https://developers.tiktok.com/docs/en/home-screen-shortcut
- https://developers.tiktok.com/docs/en/revisit-from-profile
- https://developers.tiktok.com/docs/en/basic-information-specifications
- https://developers.tiktok.com/docs/en/build-mini-games-with-unity
