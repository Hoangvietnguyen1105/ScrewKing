# NewArtUI integration

The layered PNGs in Assets/ScrewKing/Resources/Art/NewArtUI are now serialized into editable uGUI prefabs in Assets/ScrewKing/Resources/UI/Prefabs. PortGame.NewArtUi.cs only binds game state/actions to those views. See EDIT_UI_VI.md. The JPGs remain composition references; they are not rendered as screenshots over the game.

- screw_loading: mirrored wooden doors, then opening transition. Handle proportions are preserved while the top/bottom wood extends to fit the portrait viewport.
- screw_home: settings icon, wooden footer, selected Home tab, level play button and existing previous/next level selection. Shop/cards icons are dimmed and marked Soon; those systems do not exist in the current main game.
- screw_setting: painted panel, header, bolts, close button, icons, independent Music/Sound/Vibration toggles, Resume and Quit-to-home. Choices persist in PlayerPrefs. Opening settings pauses gameplay and blocks controls underneath.
- screw_win_lose: Perfect/Failed headings, screw illustration, result badge, Next level/Retry. The original per-project callbacks remain connected, including the TikTok interstitial policy and level telemetry.

The Home screw icon is currency (not level progress). Balance starts at 0, persists as ScrewKing.Currency, and each won round awards +20 once. 9099 is an example balance in the art. No spend mechanic is inferred from the demo. Result prefabs now show the wallet, +20 and rewarded x2. Completed ads add another 20 once (40 total); skipped/failed ads pay nothing. TikTok uses the base Ads/Tracking service with placement level_reward_double; native preview simulates completion with an explicit local-preview notice. See DEMO_AUDIT_VI.md.

Both ScrewKingUnity and ScrewKingTikTok use the same editable prefab layout and art assets. GameplayHUD now renders into the same Canvas below the popup siblings, so no legacy OnGUI draw calls cover the popups. TikTok SDK sources remain unchanged. Gameplay board positions and level data are unchanged.


The gameplay HUD is now authored as serialized uGUI objects as well. GameplayHUD contains Header/Progress, four HudBox instances, sixteen HudBufferSlot instances, Boosters, Guide, Message and Transition. Widget prefabs in Prefabs/Widgets own all graphics and static layout. Runtime components only bind data, activate views, instantiate prefab-based effect instances and animate named Motion nodes. There is no generic UI-element builder, GUIStyle or primitive draw pass remaining in PortGame. Flight targets are resolved from the authored slot transforms without changing the source model's timing.
