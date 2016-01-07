WinSshFS 4every1 edition
========================
 
I decided to share my clone of win-sshfs based on <https://github.com/apaka/win-sshfs>  
I did some improvments for my needs. Current devel branch version seems to be very stable.

![img](https://cloud.githubusercontent.com/assets/1085397/10747956/3f684d3a-7c18-11e5-8ca6-0f37a60426e4.jpg "UI")

There are several changes, main differences:

*   Windows 10 Support
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
