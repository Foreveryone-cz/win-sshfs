WinSshFS 4every1 edition
========================
 
I decided to share my clone of win-sshfs based on <https://github.com/apaka/win-sshfs>  
I did some improvments for my needs. Current devel branch version seems to be very stable.

Download [latest release here](https://github.com/Foreveryone-cz/win-sshfs/releases). 

![img](https://cloud.githubusercontent.com/assets/1085397/10747956/3f684d3a-7c18-11e5-8ca6-0f37a60426e4.jpg "UI")

There are several changes, main differences:

*   Windows 10 Support
*   Support for Android hosts (tested with CyanogenMod 11 [Android 4.4], requires busybox to be installed)
*   current Renci SSH (2014.4.6beta)
*   solved few bugs like payload, 2 hosts and others
*   Puttyant (Pageant) support
*   settings files location is in fixed place (%localappdata%\WinSshFS)
*   "spooldrive" - all remote hosts can by mount as mountpoint dir in one virtual drive
*   archive flag of file in windows represents and controls permission for group:
    *   ON => group have same rights as owner
    *   OFF => same rights as others)
*   Ability to use Proxy for connections
*   Send Keepalive packets. (Not configurable, each 60sec hardcoded)
*   I use different versioning: 1.5.12.5 = version.subversion.release.build

And probably others , see logs for details.

**How to install 1.6.0-rc2:**
- Download and install "DokanSetup_redist.exe" - https://github.com/dokan-dev/dokany/releases/tag/v1.0.0-RC1
- Download and install BOTH x86 and x64 - https://www.microsoft.com/download/details.aspx?id=48145
- Download "release-1.6.0-rc2.zip", unpack and run https://github.com/dimov-cz/win-sshfs/releases/tag/1.6.0
- *tested on win 8.1 home x64. Some versions of win may require switching into test mode*
