# Deployment

The live demo runs the same containers as `docker compose up` on one Linux server, with [Caddy](https://caddyserver.com/) in front for HTTPS:

```
browser ──HTTPS──▶ Caddy ──▶ nginx (client) ──▶ API ──▶ Postgres, Redis
```

Caddy gets and renews the Let's Encrypt certificate by itself. Postgres and Redis aren't reachable from outside the server.

Every push to `main` that passes CI is deployed by [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml): it builds the API and client images, pushes them to GitHub Container Registry, copies the compose files to the server over SSH, and restarts the containers on the new images. The server never builds anything.

## One-time setup

### 1. Server and domain

- An Ubuntu 24.04 server with at least 2 GB of RAM, reachable over SSH with your key.
- Ports 22, 80 and 443 open, including in the cloud provider's firewall if it has one.
- A domain name pointing at the server's IP. Without one, `<ip>.sslip.io` works too.

### 2. Prepare the server

As root (`sudo -i`):

```bash
apt update && apt upgrade -y
apt install -y docker.io docker-compose-v2

# A user for deployments, which can run Docker but isn't root
adduser --disabled-password --gecos "" deploy
usermod -aG docker deploy
mkdir -p /home/deploy/.ssh
cp ~/.ssh/authorized_keys /home/deploy/.ssh/   # or the authorized_keys of the user you logged in as
chown -R deploy:deploy /home/deploy/.ssh

ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable
```

### 3. Settings

As `deploy`, create `~/gymapp/.env`. Replace the domain and the demo password; the other secrets are generated:

```bash
mkdir -p ~/gymapp && cd ~/gymapp
cat > .env <<EOF
DOMAIN=<your domain>
DEMO_PASSWORD=<password for the demo account>
IMAGE_PREFIX=ghcr.io/mitaandrei/spotter
POSTGRES_PASSWORD=$(openssl rand -base64 24 | tr -d '/+=\n')
JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
EOF
chmod 600 .env
```

For profile pictures, also add `AWS_REGION`, `AWS_BUCKET_NAME`, `AWS_ACCESS_KEY` and `AWS_SECRET_KEY`, for an IAM user allowed only `s3:GetObject`, `s3:PutObject` and `s3:DeleteObject` on `arn:aws:s3:::<bucket>/*`.

### 4. GitHub Actions access

On your own computer, create a key used only for deployments and allow it on the server:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/spotter_deploy -N "" -C "github-actions-deploy"
ssh-copy-id -i ~/.ssh/spotter_deploy.pub deploy@<ip>
```

Then give the repository the key, the server's host key and its address:

```bash
gh secret set VPS_SSH_KEY < ~/.ssh/spotter_deploy
ssh-keyscan -t ed25519 <ip> | gh secret set VPS_KNOWN_HOSTS
gh variable set VPS_HOST --body "<ip>"
gh variable set VPS_USER --body deploy
```

### 5. First deployment

```bash
gh workflow run Deploy
```

On its first start the API creates the tables and, since `DEMO_PASSWORD` is set, the `demo` account with sample data.

## On the server

Run these in `~/gymapp` as `deploy`. Each deployment saves the running commit as `IMAGE_TAG` in `.env`, so they use the same images.

```bash
alias dc='docker compose -f docker-compose.yml -f docker-compose.prod.yml'

dc ps                     # what's running
dc logs -f api            # the API's log
dc up -d api              # restart the API after changing .env
dc exec postgres pg_dump -U gymapp gymapp > backup.sql
```

To reset the demo account, delete it and restart the API, which creates it again. Workouts have no foreign key to their user, so they are deleted first:

```bash
dc exec postgres psql -U gymapp -c "
  DELETE FROM \"Workouts\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"Users\" WHERE \"Username\" IN ('demo', 'alex'));
  DELETE FROM \"Users\" WHERE \"Username\" IN ('demo', 'alex');"
dc restart api
```
