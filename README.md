# Task Tracker Fe/Be

## About the Project
TaskTracker is a full-stack application designed to help users manage their tasks efficiently. It features a React+TS+Vite-based frontend for a dynamic user experience, a robust C# .NET backend API for business logic, and a PostgreSQL database for persistent storage. The system also includes a .NET Worker Service for handling background processes, system events, such as sending task reminders via email, utilising RabbitMQ for message queuing.

## Tech Stack

### Frontend
-   **Framework/Library:** React
-   **Language:** TypeScript
-   **Build Tool:** Vite
-   **Styling:** Tailwind CSS
-   **Routing:** React Router
-   **State Management:** React Context API

### Backend
-   **Framework:** ASP.NET Core (for Web API), .NET Worker Service
-   **Language:** C#
-   **Database:** PostgreSQL
-   **ORM:** Entity Framework Core
-   **API Documentation:** OpenAPI (NSwag and Scalar)
-   **Messaging:** MassTransit with RabbitMQ
-   **Authentication:** JWT

### Database
-   PostgreSQL

## Deployed URLs
-   **Web Application:** [https://task-tracker-web-jkxu.onrender.com](https://task-tracker-web-jkxu.onrender.com)
-   **API Documentation (Scalar):** [https://task-tracker-web-y2g5.onrender.com/scalar/](https://task-tracker-web-y2g5.onrender.com/scalar/)
-   **Worker Service (Health/Base URL):** [https://task-tracker-worker.onrender.com](https://task-tracker-worker.onrender.com) *(Note: This URL points to a hello world endpoint as it does not have a user-facing interface.)*

## Architecture Diagrams

### System Context
![System Context Diagram](https://github.com/isaacoselukwue/task-tracker-app/blob/main/architecture/structurizr-SystemContext.png?raw=true)

### Containers
![Containers Diagram](https://github.com/isaacoselukwue/task-tracker-app/blob/main/architecture/structurizr-Containers.png?raw=true)

### API Components
![API Components Diagram](https://github.com/isaacoselukwue/task-tracker-app/blob/main/architecture/structurizr-APIComponents.png?raw=true)

## License
This project is licensed under the MIT License.
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# WIP 2025