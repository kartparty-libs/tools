set PROTO_HOME=%~dp0
echo %PROTO_HOME% 
%PROTO_HOME%protoc.exe --proto_path=%PROTO_HOME% --csharp_out=../server/GameServer/src/Common %PROTO_HOME%\protocol.proto


pause