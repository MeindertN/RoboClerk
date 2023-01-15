robocopy ..\publish .\RoboClerk /MIR
docker build -t roboclerk .
docker save roboclerk | gzip > %1
rmdir roboclerk /s /q

REM to import use: docker load -i roboclerk_container.tar.gz 
REM to run the docker: docker run -a stdout -a stderr -v "I:\\temp\\":/mnt --rm roboclerk dotnet /home/RoboClerk/RoboClerk.dll -c /mnt/Roboclerk_input/RoboClerkConfig/RoboClerk.toml -p /mnt/Roboclerk_input/RoboClerkConfig/projectConfig.toml