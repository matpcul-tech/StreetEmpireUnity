# CI license setup (GameCI, Unity Personal)

GameCI activates Unity inside the build container. For Unity Personal it needs three repository secrets.

Where: https://github.com/matpcul-tech/StreetEmpireUnity/settings/secrets/actions
(repo **Settings > Secrets and variables > Actions > New repository secret**)

| Secret | Value |
| --- | --- |
| `UNITY_EMAIL` | the email of the Unity account that holds the Personal license |
| `UNITY_PASSWORD` | that account's password. GameCI advises a password without special characters. If 2FA is on, CI activation can fail |
| `UNITY_LICENSE` | the full text of a `Unity_lic.ulf` file |
| `UNITY_SERIAL` | optional. Use instead of `UNITY_LICENSE` if you only have a serial (see below) |

`GITHUB_TOKEN` is provided by GitHub. Nothing else is needed.

## The catch: Hub now writes an entitlement file, not a .ulf

Recent Unity Hub versions activate Personal as `UnityEntitlementLicense.xml` (on Linux under `~/.config/unity3d/Unity/licenses/`) and do not write `Unity_lic.ulf`. GameCI's documented input is the `.ulf`; the entitlement XML is not a documented value for `UNITY_LICENSE`. Sources:
- https://game.ci/docs/github/activation
- https://github.com/game-ci/documentation/issues/469
- https://github.com/game-ci/documentation/issues/408

Ways to get a working secret, in order:

1. **Force a .ulf from Hub.** Unity Hub > profile icon > Manage licenses > **Add**, even though a license is already listed > Get a free personal license > Agree. Then look for `Unity_lic.ulf` (Linux `~/.local/share/unity3d/Unity/`, Windows `C:\ProgramData\Unity\`, Mac `/Library/Application Support/Unity/`). If it appears, paste its full contents into `UNITY_LICENSE`. Licenses are not tied to an OS, so doing this on a Windows or Mac machine works too.
2. **Serial instead of a file.** If you have a `.ulf` anywhere (even an old one), its serial can go in `UNITY_SERIAL` and GameCI activates with email, password and serial. Reported working in issue 408. Treat it like a password.
3. **Unity Build Automation** (Unity's own cloud build) if neither works. It handles the license itself but has its own pricing.

Do not paste the entitlement XML into a secret unless GameCI documents it. Never commit any license file; `.gitignore` blocks `*.ulf`, `*.alf` and entitlement XML.

## Note on Unity's terms

Unity's Terms of Service (June 30, 2026), section 17.2(ff), restricts using AI agents and automated tools to interact with Unity services outside "Authorized Agentic Access". For that reason the Editor was not installed or activated by the agent on the box; the project files were written by hand. GameCI runs in your GitHub account under your own license, the standard way Unity projects are built in CI. Worth a read of the current terms before relying on it.
