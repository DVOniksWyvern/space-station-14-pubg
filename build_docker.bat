
REM --cache-from type=registry,ref=tnas.local:5000/ss14/pubg/server:buildcache
docker buildx build -t tnas.local:5000/ss14/pubg/mapserver:latest . --push
