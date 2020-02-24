docker stop vereesa
docker rm vereesa
docker rmi svsk/vereesa:latest
docker build ./ -t svsk/vereesa:latest 
docker run -dit --restart always --name vereesa svsk/vereesa
docker rmi $(docker images --filter "dangling=true" -q --no-trunc)