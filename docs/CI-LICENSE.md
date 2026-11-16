# CI license setup (GameCI, Unity Personal)

GameCI activates Unity inside the build container. For a Unity Personal seat, current GameCI (unity-test-runner v4.4+, unity-builder v6) needs only two repository secrets. No `.ulf` file is needed.

Where: https://github.com/matpcul-tech/StreetEmpireUnity/settings/secrets/actions
(repo **Settings > Secrets and variables > Actions > New repository secret**)

| Secret | Value |
| --- | --- |
| `UNITY_EMAIL` | the email of the Unity account that holds the Personal license |
| `UNITY_PASSWORD` | that account's password. GameCI advises a password without special characters. If the account uses two factor sign in, CI activation may fail |

`GITHUB_TOKEN` is provided by GitHub. Nothing else is needed.

How we know: the first run of this workflow (Nov 16, 2026) failed with GameCI's own list of activation strategies:

```
UNITY_EMAIL + UNITY_PASSWORD                 - Personal (free) seat
UNITY_EMAIL + UNITY_PASSWORD + UNITY_SERIAL  - Pro/Plus seat
UNITY_LICENSE or UNITY_LICENSE_FILE          - a .ulf (Enterprise/Industry)
UNITY_LICENSING_SERVER                       - floating license server
```

That matters because Unity Hub now activates Personal as `UnityEntitlementLicense.xml` and no longer writes `Unity_lic.ulf` (https://github.com/game-ci/documentation/issues/469). The older GameCI docs (https://game.ci/docs/github/activation) still describe the `.ulf` route; the action itself no longer needs it for Personal.

Do not put any license file in a secret or in the repo. `.gitignore` blocks `*.ulf`, `*.alf` and entitlement XML.

If email and password activation ever fails, the fallback is Unity Build Automation (Unity's cloud build), which has its own pricing.

## Note on Unity's terms

Unity's Terms of Service (June 30, 2026), section 17.2(ff), restricts using AI agents and automated tools to interact with Unity services outside "Authorized Agentic Access". For that reason the Editor was not installed or activated by the agent on the box; the project files were written by hand. GameCI runs in your GitHub account under your own license, the standard way Unity projects are built in CI. Worth a read of the current terms before relying on it.

## Action versions

`game-ci/unity-builder@v4` still demands a `.ulf` or a serial ("Missing Unity License File and no Serial was found"). `@v6` (Sep 2026) is a thin wrapper around game-ci/cli, the same engine `unity-test-runner@v4` now uses, and supports the email and password Personal route. Keep the builder on `@v6`.
