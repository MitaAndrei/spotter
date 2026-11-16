# Deploying the live demo to a server

The live demo runs the same containers as `docker compose up` on one Linux server, with [Caddy](https://caddyserver.com/) in front for HTTPS:

```
browser ──HTTPS──▶ Caddy ──▶ nginx (client) ──▶ API ──▶ Postgres, Redis
```

Caddy gets and renews the certificate from Let's Encrypt by itself. Postgres and Redis aren't reachable from outside the server.

After the one-time setup below, every push to `main` that passes CI is deployed by [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml). It builds the API and client images, pushes them to GitHub Container Registry, copies the compose files to the server, and restarts the containers on the new images. The server never builds anything, so a small one is enough.

## One-time setup

### 1. A server

Any Ubuntu server with 2 GB of RAM works. With the [GitHub Student Developer Pack](https://education.github.com/pack), DigitalOcean gives credit that covers a year of one:

1. **Create → Droplets**: Ubuntu 24.04 LTS, Basic, Regular, 2 GB / 1 CPU, in a region near you (e.g. Frankfurt).
2. Under **Authentication**, choose **SSH Key** and add your public key (`cat ~/.ssh/id_ed25519.pub`; create one with `ssh-keygen -t ed25519` if you have none).
3. Note the droplet's IP address.

### 2. A domain

Point an `A` record for your domain (e.g. `gymapp.example.me`) at the server's IP. The Student Pack includes a free domain from Namecheap for a year.

To start without a domain, use `<ip>.sslip.io`, for example `203.0.113.7.sslip.io`. It resolves to that IP, and Caddy can get a certificate for it.

### 3. Prepare the server

Log in as root (`ssh root@<ip>`) and run:

```bash
apt update && apt upgrade -y

# Docker, from Docker's official install script
curl -fsSL https://get.docker.com | sh

# A user for deployments, which can run Docker but isn't root
adduser --disabled-password --gecos "" deploy
usermod -aG docker deploy
mkdir -p /home/deploy/.ssh
cp ~/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh

# Only SSH and the web are open
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable

# 2 GB of swap, as headroom for Postgres and .NET on a small server
fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
```

### 4. The app's settings

Still on the server, as the `deploy` user (`su - deploy`), create `~/gymapp/.env`. Replace the domain and the demo password; the other secrets are generated:

```bash
mkdir -p ~/gymapp && cd ~/gymapp
cat > .env <<EOF
DOMAIN=gymapp.example.me
DEMO_PASSWORD=<the password shown in the README for the demo account>
IMAGE_PREFIX=ghcr.io/mitaandrei/spotter
POSTGRES_PASSWORD=$(openssl rand -base64 24 | tr -d '/+=\n')
JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
EOF
chmod 600 .env
```

To get profile picture uploads working, add `AWS_REGION`, `AWS_BUCKET_NAME`, `AWS_ACCESS_KEY` and `AWS_SECRET_KEY` too. Use an IAM user that can only read, write and delete objects in that bucket (`s3:GetObject`, `s3:PutObject` and `s3:DeleteObject` on `arn:aws:s3:::<bucket>/*`), not your own keys. Then run `dc up -d api` (see below) to restart the API with them.

### 5. Let GitHub Actions log in to the server

On your own computer, create a key used only by GitHub Actions, and allow it on the server:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/gymapp_deploy -N "" -C "github-actions-deploy"
ssh-copy-id -i ~/.ssh/gymapp_deploy.pub deploy@<ip>
```

Then give GitHub the key, the server's host key (so Actions knows it's talking to your server), and the address:

```bash
gh secret set VPS_SSH_KEY < ~/.ssh/gymapp_deploy
ssh-keyscan -t ed25519 <ip> | gh secret set VPS_KNOWN_HOSTS
gh variable set VPS_HOST --body "<ip>"
gh variable set VPS_USER --body deploy
```

### 6. First deployment

```bash
gh workflow run Deploy
```

On its first start the API creates the tables and the `demo` account with a year of workouts, two weeks of meals, and a friend called `alex` with a short chat. The demo account isn't recreated later, so changes visitors make to it stay.

The app is then at `https://<your domain>`.

## Day to day

Run these on the server, in `~/gymapp`, as `deploy`. The settings come from `.env`, where each deployment also saves `IMAGE_TAG`, the commit that is running, so these commands use the same images.

```bash
alias dc='docker compose -f docker-compose.yml -f docker-compose.prod.yml'

dc ps                  # what's running
dc logs -f api         # the API's log
dc exec postgres pg_dump -U gymapp gymapp > backup.sql   # a database backup
```

To start the demo account over, delete it and restart the API, which then creates it again. Workouts are deleted separately because they have no foreign key to their user; everything else goes with the user.

```bash
dc exec postgres psql -U gymapp -c "
  DELETE FROM \"Workouts\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"Users\" WHERE \"Username\" IN ('demo', 'alex'));
  DELETE FROM \"Users\" WHERE \"Username\" IN ('demo', 'alex');"
dc restart api
```
