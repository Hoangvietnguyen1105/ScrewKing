# Tính năng nền tảng TikTok quốc tế

Đã gắn vào **ScrewKingTikTok**. Cách làm theo bản mẫu GameFramework (module `MoodBoost.Sdk.Revisit`): **không đăng nhập, không backend**. TikTok tự ghi nhận người chơi đã làm nhiệm vụ chưa; game chỉ hỏi, trả thưởng rồi báo lại.

## Luồng

| Phần | Cách hoạt động |
|---|---|
| Home Screen | Nút Home → PopupTikTokShortcut → `TT.AddShortcut`. Kiểm tra bằng `TT.GetShortcutMissionReward`; chỉ `canReceiveReward=true` mới cho Claim. |
| Revisit | Nút Home → PopupTikTokRevisit → `TT.StartEntranceMission` hướng dẫn vào lại từ Profile. Kiểm tra bằng `TT.GetRevisitMissionReward` (TikTok 46+), máy cũ hơn dùng `TT.GetEntranceMissionReward`. |
| Trả thưởng | Cộng ốc và ghi receipt trong cùng một lần lưu, sau đó gọi `TT.ReportMissionRewardGranted` để TikTok ngừng mời nhận lại. |
| Thưởng ốc | Mặc định shortcut +100 một lần; revisit +50/ngày UTC. Chỉnh trong Settings.asset. Không thưởng vì chỉ bấm hướng dẫn. |
| Chống nhận trùng | Điều kiện do TikTok giữ; receipt local lưu bằng `TT.Save` (vốn theo từng tài khoản TikTok). Lỗi ghi dữ liệu rollback, callback cũ/bị lặp bị bỏ qua. |
| Quay lại app | Nghe TTAppLifeCycle.OnShow và fallback OnApplicationPause; kiểm tra lại điều kiện, không tự cộng trùng. |
| Lưu native | `TT.Save`/`TT.LoadSaving` cho tiến độ level, tiền, cài đặt, tiền thưởng và receipt. PlayerPrefs giữ làm mirror. |
| Tracking | Event `tiktok_growth_open/navigate/eligibility/claim/report` qua Tracking service của SDK base. |
| Compatibility | Kiểm tra CanIUse cho từng API. Tính năng không hỗ trợ được ẩn hoặc báo cần TikTok mới hơn. |

Đây là bảo vệ nhận thưởng trong game, không phải ví cloud đa thiết bị.

## Chỉnh giao diện

Các prefab uGUI ở `Assets/ScrewKing/Resources/UI/Prefabs/`:

- `PopupTikTokShortcut.prefab`
- `PopupTikTokRevisit.prefab`
- `ScreenHome.prefab`: TikTokShortcutEntry, TikTokRevisitEntry.
- `ScrewKingUI.prefab`: hai popup và các tham chiếu được nối sẵn.

Bản Editor/browser hiển thị UI xem thử theo `ShowPlatformUiInPreview`, nhưng không giả thành công API TikTok.

## Cấu hình

`Assets/ScrewKing/Resources/TikTok/Settings.asset`:

1. **AppId**: client key của game, khớp cửa sổ TikTokGame/Build Minigame.
2. **TermsUrl / PrivacyUrl**: URL HTTPS, cũng điền ở Basic Information trên Developer Portal.
3. **ShortcutReward / RevisitReward**: mặc định 100 / 50 ốc.
4. **EnableGrowthFeatures**, **AutoGuideAfterTutorial**: bật mặc định. Gợi ý tự động chỉ xuất hiện một lần khi đã xong tutorial và trở về Home.

ID quảng cáo nằm ở `Assets/SDKSetting/SdkSettings.asset`. TTSDK không hỗ trợ Application.OpenURL trong native container, nên Terms/Privacy dùng trang loading của TikTok.

## Kiểm chứng

- `TikTokCapabilitiesVerification`: ledger, rollback, nhận trùng, restart, đổi ngày, HTTPS, prefab wiring.
- `TikTokCapabilitiesPlaySmoke`: host giả lập, popup, ví, báo grant, callback cũ, resume, preview guard.

Cần xác minh trên TikTok thật: shortcut, vào lại đúng entry, nhận thưởng/lưu qua restart và đổi ngày, pause/resume.
