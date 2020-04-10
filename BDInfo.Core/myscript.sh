#!/bin/bash
mntdisk="/datadrive"
isodir="/mnt/iso"
outputftp="/home/ftpuser"

remotepath="/downloads/extract"
remoteuser="Sonic3R"
remoteport=23437
remoteip='185.56.20.10'
screenshotnum=0

if [[ ! -d "$mntdisk" ]]; then
  echo "$mntdisk does not exist !"
  exit 1
fi

echo Provide iso file name
read isofile

echo Provide folder name to extract ISO in
read foldername

echo "Generate Screens ? [y/n]"
read generatescreens


if [[ $generatescreens -eq "y" ]]; then
	echo "Number of screens"
	read ssnum
	
	if [[ $ssnum -eq 0 ]]; then
		screenshotnum=3
	else
		screenshotnum=$ssnum
	fi
fi

[ ! -d "$isodir" ] && sudo mkdir "$isodir"

if [ ! -f "$mntdisk/$isofile" ]; then
	echo Copying ISO from remote
	scp -r -P $remoteport $remoteuser@$remoteip:$remotepath/$isofile $mntdisk/$isofile
fi

if [[ $? -eq 1 ]]; then
	echo Copying ISO failed
	exit 1;
fi

if [ ! -d "$mntdisk/$foldername" ]; then
	echo Mounting $mntdisk/$isofile
	sudo mount -o loop $mntdisk/$isofile $isodir

	if [[ $? -eq 1 ]]; then
		echo Mounting ISO failed
		exit 1;
	fi

	echo Generate bd info
	dotnet $mntdisk/bdinfo/BDInfo.dll -p $isodir -r $outputftp -o "${foldername}.txt"

	if [[ $? -eq 1 ]]; then
		echo Generating info failed
		exit 1;
	fi

	echo Creating $foldername
	mkdir $mntdisk/$foldername

	if [[ $? -eq 1 ]]; then
		echo Creating folder $foldername failed
		exit 1;
	fi

	echo Copying ISO content to $foldername
	scp -r $isodir/* $mntdisk/$foldername/

	if [[ $? -eq 1 ]]; then
		echo Copying ISO content to $foldername failed
		exit 1;
	fi

	echo Unmount ISO
	sudo umount $isodir

	if [[ $? -eq 1 ]]; then
		echo Unmounting ISO failed
		exit 1;
	fi
fi

if [[ $generatescreens -eq "y" ]]; then
	pkgs='ffmpeg'
	if ! dpkg -s $pkgs >/dev/null 2>&1; then
	  echo Installing $pkgs
	  sudo apt-get install $pkgs
	fi

	bigfile="$(find $mntdisk/$foldername/ -printf '%s %p\n'| sort -nr | head -1 | sed 's/^[^ ]* //')"

	echo "Movie found: $bigfile"

	movieseconds=$(ffmpeg -i $bigfile 2>&1 | grep "Duration"| cut -d ' ' -f 4 | sed s/,// | sed 's@\..*@@g' | awk '{ split($1, A, ":"); split(A[3], B, "."); print 3600*A[1] + 60*A[2] + B[1] }')
	period=$((movieseconds/screenshotnum))
	period=$(( $period - 100 ))

	echo "Movie seconds: $movieseconds"
	echo "Ss num: $screenshotnum"
	echo "Period $period"

	i=1;
	while [[ $i -le $screenshotnum ]]
	do
		seconds=$(( period * i ))
		echo "Seconds $seconds"

		ffmpeg -ss $seconds -t 1 -i $bigfile -vcodec png -vframes 1 "${outputftp}/${foldername}_${i}.png"
		i=$(( $i + 1 ))
	done
fi

if [[ $? -eq 1 ]]; then
	echo Generating screens failed
	exit 1;
fi

echo Copying to seedbox
scp -r -P $remoteport $mntdisk/$foldername $remoteuser@$remoteip:$remotepath/

if [[ $? -eq 1 ]]; then
	echo Copying to seedbox failed
	exit 1;
fi

echo Delete $foldername
sudo rm -rf $mntdisk/$foldername

if [[ $? -eq 1 ]]; then
	echo Removing $foldername failed
	exit 1;
fi

echo Delete $isofile
sudo rm $mntdisk/$isofile

if [[ $? -eq 1 ]]; then
	echo Removing $isofile failed
	exit 1;
fi

echo Done