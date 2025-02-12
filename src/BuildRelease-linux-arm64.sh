echo -e "Building with target \033[94mLinux-arm64\033[0m"

echo
echo -e "\033[104m\033[97m Deleting previous build... \033[0m"

rm -rf Release/linux-arm64/*


echo
echo -e "\033[104m\033[97m Building AliFilter... \033[0m"

cd AliFilter
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/linux-arm64.pubxml
cd ..

echo
echo -e "\033[104m\033[97m Removing additional files... \033[0m"

rm Release/linux-arm64/AliFilter.dbg
rm Release/linux-arm64/AliFilter.xml
rm Release/linux-arm64/Accord.dll.config

echo
echo -e "\033[94mDone!\033[0m"
