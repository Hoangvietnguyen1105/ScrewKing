# Screw King lên TikTok quốc tế

Bản chuyển đổi nằm trong ScrewKingTikTok. Đã áp dụng SDK MoodBoost, TTSDK, UniTask và VContainer đúng bản GameFrameworkMini. Các file SDK giữ nguyên; phần gameplay Screw King nối vào dịch vụ quảng cáo, tracking, remote config defaults, lifecycle và rung của base.

## Cấu hình khi có ID

1. Mở project ScrewKingTikTok. Vào MoodBoost > SDK Settings để mở Assets/SDKSetting/SdkSettings.asset. Điền mediation/ad-unit ID riêng của Screw King tại đây. Asset đã được gắn vào scene; không dùng ID quảng cáo của game mẫu.
2. Đăng ký Mini Game TikTok quốc tế để có public App ID. Điền cùng App ID trong TikTokGame/Build Minigame và Resources/TikTok/Settings.asset (tạo bằng Create > Screw King > TikTok Settings). Không đưa client secret vào project.
3. Chọn WebGL, bật MB_TIKTOK. Screw King > TikTok > Build TikTok Package kiểm tra cấu hình và mở công cụ TTSDK để chuyển đổi/đóng gói. Menu này chưa tự build hoặc upload.
4. Kiểm tra gói tt-minigame trên DevTool và điện thoại rồi mới upload/gửi xét duyệt.

## Đã kiểm tra

- SDK MoodBoost 255 file, TTSDK 104 file, UniTask 337 file: không khác nội dung so với base; VContainer cùng commit.
- Tracking màn chơi, thời gian, chơi lại, revive, booster và sự kiện quảng cáo qua dịch vụ gốc của base.
- Play mode: bỏ qua video không nhận thưởng, xem hết nhận đúng một lần, âm thanh phục hồi, interstitial bị chặn ở màn đầu theo chính sách base.
- Nhánh MB_TIKTOK biên dịch thành công. 89 màn x 2 seed và 180 bước physics đối chiếu đạt.
- WebGL tối ưu build thành công, khoảng 53.3 MB; chơi thắng màn 1 và chuyển màn 2 được trên trình duyệt, không thấy lỗi console.

Chơi thử tại http://127.0.0.1:8769 khi máy chủ đang chạy. Khởi động lại bằng python3 Tools/serve_preview.py từ thư mục ScrewKingTikTok. Browser preview dùng null provider của base; chỉ Editor dùng quảng cáo giả để kiểm tra luồng.

## Chưa xác nhận trên TikTok thật

Chưa có App ID/ad ID nên chưa thử gửi tracking lên TikTok, quảng cáo thật hoặc đóng gói/upload. Cần kiểm tra callback quảng cáo, vào nền/quay lại, lưu dữ liệu, touch/safe area, bộ nhớ, tốc độ tải và kích thước gói sau chuyển đổi. Project đang dùng Unity 6000.3.21f1; tương thích host TikTok vẫn cần xác nhận với công cụ/tài liệu chính thức.

Các giới hạn sẵn có của base được giữ nguyên: remote config dùng backend null, lifecycle dựa OnApplicationPause; lớp game đã bổ sung login, shortcut, revisit và native save ngày 24/10/2026, xem TIKTOK_CAPABILITIES_VI.md để cấu hình và kiểm tra các điều kiện phát hành còn thiếu.

Tài liệu: https://developers.tiktok.com/docs/en/build-mini-games-with-unity

UI is now authored in uGUI prefabs, including the gameplay HUD. See EDIT_UI_VI.md for direct prefab editing. Runtime code only binds state and animates Motion nodes. Latest browser output: 53419806 bytes.

Platform capabilities (24/10/2026): see [TIKTOK_CAPABILITIES_VI.md](TIKTOK_CAPABILITIES_VI.md). Prefabs and code are wired; live credentials/backend/Portal setup and a supported native release toolchain remain required.
