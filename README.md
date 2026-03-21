**How to Run This Project **

✅ 1. Install Required Software
🖥 Visual Studio 2022 Community

Download:
https://visualstudio.microsoft.com/

During installation, select:

✔️ ASP.NET and Web Development

⚙ .NET 8 SDK

Download:
https://dotnet.microsoft.com/download

Verify installation:
dotnet --version

🐳 Docker Desktop
Download:
https://www.docker.com/products/docker-desktop/

After installation, ensure Docker is running.
Verify:
docker --version

☁ Azure Cosmos DB Emulator
Download:
https://learn.microsoft.com/azure/cosmos-db/local-emulator

After installation:
Start the Emulator
Open:
https://localhost:8081/_explorer/index.html


Download and install the certificate as Trusted Root
📥 2. Clone the Repository (Visual Studio)
Open Visual Studio

Click Git → Clone Repository
Paste the repository URL:
https://github.com/Sabrina82-LS/DeliInventoryManagement_1.git
Choose a local folder and click Clone
The solution will open automatically.

🐇 3. Start RabbitMQ (Docker)
Open a terminal in the project root (where docker-compose.yml is located).
Run:
docker compose up -d

Verify containers:
docker ps

🔐 Open RabbitMQ Management UI
Open in browser:
http://localhost:15672
Login:
Username: admin
Password: admin123


If login fails, reset:
docker compose down -v
docker compose up -d

☁ 4. Ensure Cosmos Emulator is Running
Before starting the API, make sure the Cosmos DB Emulator is open.

▶ 5. Configure Startup Projects (Visual Studio)
Right-click the Solution
Select Properties
Choose:

✔️ Multiple startup projects
Set:
DeliInventoryManagement_1.Api → Start
DeliInventoryManagement_1.Blazor → Start
Click Apply → OK

▶ 6. Run the Application
Press:
F5

or click the green ▶ Run button.
Two browser windows will open:
1️⃣ Swagger UI (API) – for testing endpoints
2️⃣ Blazor Web App – main application

🐇 RabbitMQ Behavior (Automatic)
When the API starts, it automatically:
Connects to RabbitMQ
Creates exchange
Creates main queues
Creates retry queues
Creates dead letter queues
Starts background consumers
Starts Outbox Dispatcher
Queues created automatically:
Main Queues
sale.created
restock.created
Retry Queues
sale.created.retry
restock.created.retry
Dead Letter Queues
sale.created.dlq
restock.created.dlq

🛠 Troubleshooting
Reset RabbitMQ:
docker compose down -v
docker compose up -d

Check running containers:
docker ps

View Rabbit logs:
docker logs rabbitmq
