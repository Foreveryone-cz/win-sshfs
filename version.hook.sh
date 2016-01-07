#!/bin/bash
#Git Post Merge Hook
#---------------------
#Gets the latest tag info from the git repo and updates the AssemblyInfo.cs file with it.
#This file needs to be place in the .git/hooks/ folder and only works when a git pull is
#made which contains changes in the remote repo.

PRODUCT="WinSshFS 4every1 edition"

#get the latest tag info. The 'always' flag will give you a shortened SHA1 if no tag exists.
tag=$(git describe --tags --long)

#tag="A.B.C.D-X-hash"
echo $tag

AI="Sshfs/Sshfs/Properties/AssemblyInfo.cs"

#If no tag has been added only the sha1 will be returned
if [[ $tag=="*.*" ]]
then
	IFS='-' read -ra PARTS <<< "$tag"

	IFS='.' read -ra TAG <<< "${PARTS[0]}"
	#echo "${TAG[2]}"
	#echo "${TAG[3]}"

	IFS='-' read -ra COMMITS <<< "${PARTS[1]}"
	#echo "${COMMITS[0]}"

	#This will be the version in the format <major>.<minor>.<build number> (.<revision> remove revision, amend of version inside will be ok with this)
	version="${TAG[0]}"."${TAG[1]}"."${TAG[2]}"."${TAG[3]}"
	echo $version

	#Update the AssemblyVersion and AssemblyFileVersion attribute with the 'version'
	sed -i.bak "s/\AssemblyVersion(\".*\")/AssemblyVersion(\"$version\")/g" $AI 2>/dev/null
	sed -i.bak "s/\AssemblyFileVersion(\".*\")/AssemblyFileVersion(\"$version\")/g" $AI 2>/dev/null
	sed -i.bak "s/AssemblyProduct(\".*\")/AssemblyProduct(\"$PRODUCT $version-${COMMITS[0]}\")/g" $AI 2>/dev/null
	#cat $AI
fi
