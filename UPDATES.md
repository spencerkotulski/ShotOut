# Update system

Users: Settings > Updates contains Automatically check for updates (on by default), Check now, and GitHub releases. Checks run in the background at app opening and every 24 hours while running. A newer stable version adds a dismissible banner at the top of the editor. View update opens the official release page, where users read notes and download/run the existing NSIS installer. No account or token is required. No automatic installation or app shutdown occurs.

Maintainer steps for the next explicitly requested release:
1. Prepare the chosen release version in the app and installer, with changelog and verified builds, using the existing release workflow. Preview iterations keep the current release version and do not trigger an update.
2. Push the source, create the matching tag (for example v1.4.3 only when that version is selected), and publish a normal GitHub release in spencerkotulski/ShotOut. Mark it as the latest release; do not mark it as a draft or pre-release.
3. Upload the branded ShotOut-Setup-vVERSION.exe installer, optional portable ZIP, source ZIP and SHA256SUMS.txt to Assets; put the changelog in the description.
4. The API endpoint /repos/spencerkotulski/ShotOut/releases/latest then supplies the published version. Older apps with this checker offer the release page automatically; no separate API hosting, credentials, or update manifest is needed.
5. Test with the previous installed version and confirm its banner, release notes link, and installer upgrade before announcing broadly.

API documentation: https://docs.github.com/en/rest/releases/releases#get-the-latest-release
Repository releases: https://github.com/spencerkotulski/ShotOut/releases

Validation: local compiler and focused version/URL/banner/settings and simulated Print Screen handoff checks passed. The public API is checked for a newer stable release. A real newer release, browser interaction and installed upgrade were not exercised. This is automatic update notification; a self-installing updater would be a separate future feature.

Network behavior: standard HTTPS request with application User-Agent, bounded response and timeouts, no screenshot/file data or account token. Offline/rate-limit errors remain quiet automatically and are explained in manual checks. Draft/pre-release and unparseable preview tags are ignored; equal/older versions never prompt. Release links must belong to this repository on github.com.

Print Screen setup is now under Settings > Experimental; its behavior and existing preferences are preserved.

