<p align="center">
  <img width="150" src=".github/images/logo.png"/>
</p>

<h1 align="center">KanbanCord</h1>

<p align="center">
  A simple Kanban board, but on Discord. The idea came from <a href="https://github.com/seansylee" target="_blank">seansylee</a> who made <a href="https://github.com/seansylee/kanban-board-bot" target="_blank">kanban-board-bot</a> which is no longer maintained.
</p>

## Table of Contents

<!-- TOC -->
  * [Table of Contents](#table-of-contents)
  * [To-Do](#to-do)
  * [Setup](#setup)
    * [Docker](#docker)
      * [Variables](#variables)
      * [Database](#database)
<!-- TOC -->

## To-Do

- [x] Add item to board, `/add` will open a modal with title for the item and long description.
- [x] View item by Id `/view <id>` or `/show <id>`
- [x] Show board `/board`
- [x] Remove item by id `/delete <id>`
- [x] Start task `/start <id>` moves item from backlog to in-progress
- [x] Complete task `/complete <id>` moves item from in-progress to completed/done
- [x] Clear board, deletes everything on the board. Should have a confirmation button
- [x] Move item to specified row `/move <id> <row>` where row could be "Backlog", "In Progress" and "Completed"
- [ ] Assign tasks to users
- [ ] Add priorities
- [ ] Search for items by keyword
- [ ] Import/Export
- [ ] Add comments to task
- [x] Edit task `/edit <id>` with ability to edit title and description

## Setup

### Docker

#### Variables

These variables are Environment variables

| Variable                    | Description                                                             | Required | Default value |
|-----------------------------|-------------------------------------------------------------------------|----------|---------------|
| `TOKEN`                     | Your discord application token (bot token).                             | Yes      | None          |
| `MONGODB_CONNECTION_STRING` | MongoDB Connection String eg. `mongodb://localhost:27017`.              | Yes      | None          |
| `MONGODB_DATABASE_NAME`     | MongoDB Database Name, if you want to change it from the default value. | No       | `KanbanCord`  |


#### Database

A [MongoDB](https://www.mongodb.com/) is required for this bot to run. [A Docker image is available here](https://hub.docker.com/r/mongodb/mongodb-community-server).

KanbanCord will automatically create the required collections on startup, if they are missing.

| Collection Name |
|-----------------|
| Tasks           |
| Settings        |
