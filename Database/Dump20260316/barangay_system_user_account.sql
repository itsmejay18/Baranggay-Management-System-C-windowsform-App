-- MySQL dump 10.13  Distrib 8.0.40, for Win64 (x86_64)
--
-- Host: localhost    Database: barangay_system
-- ------------------------------------------------------
-- Server version	8.0.39

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `user_account`
--

DROP TABLE IF EXISTS `user_account`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_account` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `barangay_id` int NOT NULL,
  `username` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `resident_id` int DEFAULT NULL,
  `full_name` varchar(150) DEFAULT NULL,
  `contact_no` varchar(50) DEFAULT NULL,
  `email` varchar(150) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `last_login_at` datetime DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `photo_url` varchar(255) DEFAULT NULL,
  `first_name` varchar(100) DEFAULT NULL,
  `middle_name` varchar(100) DEFAULT NULL,
  `last_name` varchar(100) DEFAULT NULL,
  `position` varchar(100) DEFAULT NULL,
  `department` varchar(100) DEFAULT NULL,
  `last_project` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  KEY `barangay_id` (`barangay_id`),
  KEY `resident_id` (`resident_id`),
  CONSTRAINT `user_account_ibfk_1` FOREIGN KEY (`barangay_id`) REFERENCES `barangay` (`barangay_id`) ON DELETE CASCADE,
  CONSTRAINT `user_account_ibfk_2` FOREIGN KEY (`resident_id`) REFERENCES `resident` (`resident_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_account`
--

LOCK TABLES `user_account` WRITE;
/*!40000 ALTER TABLE `user_account` DISABLE KEYS */;
INSERT INTO `user_account` VALUES (2,1,'Janelle','8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92',NULL,'',NULL,NULL,1,NULL,'2026-02-07 11:48:28','2026-02-11 14:06:23','C:\\Users\\Loona\\Downloads\\d0258d35-9358-45a1-b1f4-cd18e9e9b2e2.jpg',NULL,NULL,NULL,NULL,NULL,NULL),(3,1,'daryll','8bb0cf6eb9b17d0f7d22b456f121257dc1254e1f01665370476383ea776df414',NULL,'daryll',NULL,NULL,1,'2026-02-18 18:17:42','2026-02-11 12:42:26','2026-02-18 10:17:42',NULL,NULL,NULL,NULL,NULL,NULL,NULL),(4,1,'daryll2','v1.100000.dAOjJ+H8Onq788lFWOnprQ==.0+w9F2M7vNbZi4NkAG+Vhnl0fjriU2xu0wiAvKY/YUk=',NULL,'daryll2',NULL,NULL,1,NULL,'2026-02-18 00:08:25','2026-02-18 00:08:25','storage/profile-photos/ed19b0e858414884b758aabe73b887c5.png',NULL,NULL,NULL,NULL,NULL,NULL),(5,1,'daryll24','v1.100000.8Jz8EKIXS9IL4gfXwQZnnA==.rWlO0/aBN0Q1j2rom0TAUVhWVxFo6w0d7jUrt13tGZ0=',NULL,'daryll24',NULL,NULL,1,NULL,'2026-02-18 00:14:19','2026-02-18 00:14:19',NULL,NULL,NULL,NULL,NULL,NULL,NULL),(7,1,'admin','v1.100000.MURu1hmxbjzirejeInxAVQ==.5Q61lAVbeSueUJTQQGyM38e9VVxmRVBIq5PbbegsQHE=',NULL,NULL,'','',1,'2026-02-18 08:47:14','2026-02-18 00:19:40','2026-02-18 10:27:41','C:\\Users\\Loona\\Downloads\\136bb18c-aa50-4589-b21c-6bfd6739c050.jpg','','','','','','');
/*!40000 ALTER TABLE `user_account` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:23
