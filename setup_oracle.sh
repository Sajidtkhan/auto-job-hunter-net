#!/bin/bash
set -e
echo "========================================================"
echo "  AutoJobHunter.NET - Oracle Cloud 24/7 Auto Deployment  "
echo "========================================================"

# Update and install dependencies
sudo apt-get update
sudo apt-get install -y wget git curl iptables-persistent

# Install Microsoft .NET 9 SDK
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET 9 SDK..."
    UBUNTU_VER=$(lsb_release -rs)
    wget -q https://packages.microsoft.com/config/ubuntu/${UBUNTU_VER}/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-9.0
fi

# Clone or update repository
APP_DIR="/opt/autojobhunter"
sudo mkdir -p $APP_DIR
sudo chown -R $USER:$USER $APP_DIR

cd $APP_DIR
if [ ! -d ".git" ]; then
    echo "Cloning repository..."
    git clone https://github.com/Sajidtkhan/auto-job-hunter-net.git .
else
    echo "Pulling latest code..."
    git pull origin main
fi

# Build Project
echo "Building .NET 9 application..."
dotnet publish -c Release -o ./publish

# Create 24/7 Systemd Background Service
echo "Creating 24/7 systemd background service..."
sudo tee /etc/systemd/system/autojobhunter.service > /dev/null <<EOF
[Unit]
Description=AutoJobHunter.NET 24/7 Automated Job Hunter
After=network.target

[Service]
WorkingDirectory=${APP_DIR}/publish
ExecStart=/usr/bin/dotnet ${APP_DIR}/publish/JobAutoApplyNET.dll
Restart=always
RestartSec=10
SyslogIdentifier=autojobhunter
User=${USER}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=PORT=5000

[Install]
WantedBy=multi-user.target
EOF

# Reload and Start Service
sudo systemctl daemon-reload
sudo systemctl enable autojobhunter.service
sudo systemctl restart autojobhunter.service

# Open Oracle Cloud iptables port 5000 & 80
echo "Opening firewall port 5000..."
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 5000 -j ACCEPT || sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
sudo netfilter-persistent save 2>/dev/null || true

PUBLIC_IP=$(curl -s ifconfig.me || hostname -I | awk '{print $1}')
echo "========================================================"
echo "  DEPLOYMENT SUCCESSFUL! 24/7 SERVICE IS ACTIVE!        "
echo "  Dashboard URL: http://${PUBLIC_IP}:5000               "
echo "  Resume URL:    http://${PUBLIC_IP}:5000/resume.pdf    "
echo "  Daily Schedule: 9:00 AM IST Auto-apply is running!    "
echo "========================================================"
