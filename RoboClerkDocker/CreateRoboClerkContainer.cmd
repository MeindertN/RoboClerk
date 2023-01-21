REM before running this batch file, make sure to build the solution in the ReleasePublish configuration. It will create a "publish" dir that is used by this script to create the container.

REM copy all the files from the publish directory to the location of this batch file
robocopy ..\publish .\RoboClerk /MIR
REM build the container
docker build -t roboclerk .
REM save the container to the location indicated on the commandline
docker save roboclerk | gzip > %1
REM remove the local roboclerk directory that we copied at the beginning of the script
rmdir Roboclerk /s /q

REM to import the container use: docker load -i roboclerk_container.tar.gz 
REM to run the docker: docker run -a stdout -a stderr -v "I:\\temp\\":/mnt --rm roboclerk dotnet /home/RoboClerk/RoboClerk.dll -c /mnt/Roboclerk_input/RoboClerkConfig/RoboClerk.toml -p /mnt/Roboclerk_input/RoboClerkConfig/projectConfig.toml