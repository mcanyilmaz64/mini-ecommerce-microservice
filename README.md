Bu proje; ürün listeleme, sepete ürün ekleme ve sipariş oluşturma gibi temel e-ticaret fonksiyonlarını içeren  microservice mimarisi ile geliştirilmiş modern bir web uygulamasıdır.

frontend (Angular)
        │
        ▼
   gateway (YARP)
        │
 ┌──────┴───────────────┐
 │          │           │
 ▼          ▼           ▼
products    carts       orders
(MSSQL)   (PostgreSQL) (MongoDB)


<img width="1416" height="858" alt="Login" src="https://github.com/user-attachments/assets/f7b1d447-13a0-4263-9911-d912c1f22565" />
<img width="1565" height="890" alt="Ana sayfa" src="https://github.com/user-attachments/assets/cfcba9a7-adb3-4921-9325-b2e81a1fd163" />
<img width="1411" height="738" alt="Sepetim" src="https://github.com/user-attachments/assets/39b8a7d0-889d-4aad-990f-799f6ae2b10c" />
<img width="1515" height="873" alt="Sipariş Listem" src="https://github.com/user-attachments/assets/8bd14c60-4f46-44a3-a37c-dda462e7b5f2" />
| Katman / Bileşen               | Teknoloji                             |
| ------------------------------ | ------------------------------------- |
| **Frontend**                   | Angular                               |
| **API Gateway**                | YARP (Reverse Proxy)                  |
| **Products Service**           | .NET 8 Web API + MSSQL + EF Core      |
| **Shopping Carts Service**     | .NET 8 Web API + PostgreSQL + EF Core |
| **Orders Service**             | .NET 8 Web API + MongoDB Driver       |
| **Deployment**                 | Docker & Docker Compose               |
| **Authentication (opsiyonel)** | JWT / Role-Based Auth *(eklediysen)*  |
| **Mimari**                     | Microservices Architecture            |
