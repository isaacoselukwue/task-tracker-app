"""
Task‑Tracker System diagrams using the **diagrams** python library.

▸ Generates three SVG/PNG diagrams side‑by‑side:
   1. System Context – people + external system interactions
   2. Container View – internal containers & data‑flow
   3. Deployment View (development docker setup)

Run with:
    pip install diagrams            # once
    python task_tracker_diagram.py  # outputs *.png next to the script

Requires Graphviz installed and in PATH.
"""

from diagrams import Diagram, Cluster
from diagrams.onprem.client import User
from diagrams.onprem.compute import Server
from diagrams.onprem.database import PostgreSQL
from diagrams.onprem.queue import Rabbitmq
from diagrams.onprem.client import Users as WebBrowserIcon
from diagrams.programming.framework import React
from diagrams.programming.language import Csharp
from diagrams.generic.blank import Blank
SaaS = Blank


###############################################################################
# 1 ▸ System‑context diagram
###############################################################################

with Diagram(
    "Task Tracker System – System Context",
    filename="task_tracker_system_context",
    show=False,
    direction="LR",
):
    user_actor = User("User")
    admin_actor = User("Administrator")

    task_tracker_system = Server("Task‑Tracker System\n[Software System]")
    email_service_external = SaaS("Email Service\n[External]")

    user_actor >> task_tracker_system
    admin_actor >> task_tracker_system
    task_tracker_system >> email_service_external

###############################################################################
# 2 ▸ Container diagram
###############################################################################

with Diagram(
    "Task Tracker System – Containers",
    filename="task_tracker_system_containers",
    show=False,
    direction="TB",
):
    user_actor_cont = User("User")
    admin_actor_cont = User("Administrator")

    with Cluster("Task‑Tracker System\n(taskTrackerSystem)"):
        frontend_container = React("Frontend Application\n[Container]\nReact, TypeScript")
        backend_api_container = Csharp("Backend API\n[Container]\nASP.NET Core")
        worker_service_container = Csharp("Worker Service\n[Container]\n.NET Worker")
        db_container = PostgreSQL("Database\n[Container]\nPostgreSQL")
        queue_container = Rabbitmq("Message Queue\n[Container]\nRabbitMQ")

        user_actor_cont >> frontend_container
        admin_actor_cont >> frontend_container
        frontend_container >> backend_api_container
        backend_api_container >> db_container
        backend_api_container >> queue_container
        worker_service_container >> db_container
        worker_service_container << queue_container

    email_service_cont = SaaS("Email Service\n[External]")
    worker_service_container >> email_service_cont

###############################################################################
# 3 ▸ Deployment (dev docker) diagram
###############################################################################

with Diagram(
    "Task Tracker System – Deployment (Dev)",
    filename="task_tracker_system_deployment",
    show=False,
    direction="LR",
):
    with Cluster("User's Machine\n[Deployment Node]"):
        browser_instance = WebBrowserIcon("Web Browser\n(hosts Frontend Application)")

    with Cluster("Docker Host\n[Deployment Node]"):
        api_docker_container = Csharp("Backend API\n[Docker Container]\ntasktracker-api")
        worker_docker_container = Csharp("Worker Service\n[Docker Container]\ntasktracker-worker")
        db_docker_container = PostgreSQL("Database\n[Docker Container]\npostgres")
        mq_docker_container = Rabbitmq("Message Queue\n[Docker Container]\nrabbitmq")

    email_service_deploy = SaaS("Email Service\n[External System]")

    browser_instance >> api_docker_container
    api_docker_container >> db_docker_container
    api_docker_container >> mq_docker_container
    worker_docker_container >> db_docker_container
    worker_docker_container >> mq_docker_container
    worker_docker_container >> email_service_deploy