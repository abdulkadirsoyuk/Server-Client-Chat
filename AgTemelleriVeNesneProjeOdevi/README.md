This project enables server-client communication using C# forms. Messages sent by clients can be viewed by all clients connected to the server.
It validates user logins through a Microsoft Azure-hosted SQL database. Additionally, the server allows user management functionalities like adding and removing users.
During message transmission, messages are encrypted using the Vigenere cipher method and decrypted on the server before being displayed on screen.
Incoming messages are stored in a text file, allowing the capability to view all messages.
!!! If SQL errors occur in the project, it is likely due to the omission of adding the necessary IP address for Azure security.



Bu proje, C# form kullanarak geliştirilen bir uygulama ile server-client iletişimini sağlar. Client tarafından gönderilen mesajlar, servere bağlı tüm client'ler tarafından görülebilir hale getirilmiştir.
Kullanıcı girişlerini Microsoft Azure'a bağlı SQL veritabanı üzerinden kontrol eder. Ayrıca, server üzerinden kullanıcı ekleme ve silme özellikleri mevcuttur.
Mesaj iletimi sırasında, Vigenere şifreleme yöntemi kullanılarak mesajlar şifrelenir ve server tarafında çözülerek ekrana yazdırılır.
Gelen mesajlar bir metin dosyasında tutulur ve tüm mesajları görme özelliği bulunur. 
!!! Projede SQL hatası alınırsa muhtemelen Azure güvenliği için gerekli IP adresi eklenmemiştir.