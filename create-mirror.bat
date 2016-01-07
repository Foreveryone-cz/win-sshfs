mkdir mirrored
cd mirrored
mkdir testdir
cd ..
"%PROGRAMFILES(x86)%\Dokan\DokanLibrary\sample\mirror\mirror.exe" /r mirrored /l t /t 1 /d /s
