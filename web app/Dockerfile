# Build Angular app
FROM node:18-alpine AS build
WORKDIR /app

# Copy package.json và package-lock.json trước, tránh cài lại dependencies khi không cần thiết
COPY package.json package-lock.json ./
RUN npm install --force

# Sau khi cài xong mới copy toàn bộ source code vào
COPY . .

# Build production với tối ưu hóa
RUN npm run build --configuration=production

# Serve với Nginx
FROM nginx:alpine

# Copy file build từ bước trước vào thư mục Nginx
COPY --from=build /app/dist/coreui-free-angular-admin-template/browser /usr/share/nginx/html

# Copy file config Nginx nếu cần
COPY nginx.conf /etc/nginx/nginx.conf
COPY default.conf /etc/nginx/conf.d/default.conf

# Tạo cache cho Nginx
RUN mkdir -p /var/cache/nginx && chmod 777 /var/cache/nginx

# Expose cổng Nginx
EXPOSE 5025
CMD ["nginx", "-g", "daemon off;"]
