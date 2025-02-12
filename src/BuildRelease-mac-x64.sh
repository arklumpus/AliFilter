#!/usr/bin/env zsh

if [ "$#" -ne 4 ]; then
    echo "This script requires the following parameters:"
    echo "    Developer ID Application signing identity"
    echo "    Apple ID (email address)"
    echo "    App-specific password for the Apple ID"
    echo "    Developer team ID"
    exit 64
fi

echo -e "Building with target \033[94mmacOS-x64\033[0m"

echo
echo -e "\033[104m\033[97m Deleting previous build... \033[0m"

rm -rf Release/macOS-x64/*


echo
echo -e "\033[104m\033[97m Building AliFilter... \033[0m"

cd AliFilter
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/macOS-x64.pubxml
cd ..

echo
echo -e "\033[104m\033[97m Removing additional files... \033[0m"

rm -rf Release/macOS-x64/AliFilter.dsym
rm Release/macOS-x64/AliFilter.xml
rm Release/macOS-x64/Accord.dll.config

echo
echo -e "\033[104m\033[97m Signing excutable \033[0m"

chmod +x Release/macOS-x64/AliFilter
codesign --deep --force --timestamp --options=runtime --entitlements="AliFilter/Properties/PublishProfiles/AliFilter.entitlements" --sign "$1" "Release/macOS-x64/AliFilter"
codesign --verify -vvv --strict --deep "Release/macOS-x64/AliFilter"

echo
echo -e "\033[104m\033[97m Notarizing executable \033[0m"

cd Release/macOS-x64

rm -f "AliFilter.zip"

ditto -ck --rsrc --sequesterRsrc --keepParent "AliFilter" "AliFilter.zip"

xcrun notarytool submit AliFilter.zip --apple-id "$2" --password "$3" --team-id "$4" --wait

spctl -a -vvv -t install AliFilter

rm -f "AliFilter.zip"

cd ../..

echo
echo -e "\033[94mDone!\033[0m"
