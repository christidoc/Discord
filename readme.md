docker build -t my-discord-bot .
docker run --rm \
  -e DISCORD_TOKEN \
  my-discord-bot

or:
docker compose up --build -d
docker compose logs -f discord-bot