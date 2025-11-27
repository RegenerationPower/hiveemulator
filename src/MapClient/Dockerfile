FROM node:20-alpine

# Робоча директорія всередині контейнера
WORKDIR /app

# Спочатку тільки package-файли — кеш інсталяції залежностей
COPY package.json package-lock.json* ./
RUN npm install

# Потім — увесь код
COPY . .

# Vite-сервер слухає на 3000 (як у твоєму vite.config.js)
EXPOSE 3000

# Запуск dev-сервера Vite
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]