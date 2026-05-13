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
-- Table structure for table `activity_log`
--

DROP TABLE IF EXISTS `activity_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `activity_log` (
  `log_id` int NOT NULL AUTO_INCREMENT,
  `resident_id` int NOT NULL,
  `module` varchar(40) NOT NULL,
  `action` varchar(50) NOT NULL,
  `details` varchar(255) DEFAULT NULL,
  `action_by` int DEFAULT NULL,
  `action_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`log_id`),
  KEY `idx_activity_resident` (`resident_id`),
  KEY `idx_activity_module` (`module`)
) ENGINE=InnoDB AUTO_INCREMENT=52 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `activity_log`
--

LOCK TABLES `activity_log` WRITE;
/*!40000 ALTER TABLE `activity_log` DISABLE KEYS */;
INSERT INTO `activity_log` VALUES (1,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:51:50'),(2,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:51:56'),(3,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:52:14'),(4,13,'Residents','Updated','Profile updated',1,'2026-02-04 14:52:25'),(5,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:53:36'),(6,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:53:44'),(7,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:55:30'),(8,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:55:35'),(9,12,'Residents','Updated','Profile updated',1,'2026-02-04 14:55:48'),(10,12,'Residents','Updated','Profile updated',1,'2026-02-04 15:08:19'),(11,12,'Residents','Updated','Profile updated',1,'2026-02-04 15:08:47'),(12,12,'Residents','Updated','Profile updated',1,'2026-02-04 15:08:58'),(13,12,'Residents','Updated','Profile updated',1,'2026-02-04 15:11:33'),(14,12,'Residents','Updated','Profile updated',1,'2026-02-04 15:24:48'),(15,12,'Residents','Updated','Profile updated',1,'2026-02-04 17:18:38'),(16,12,'Residents','Updated','Profile updated',1,'2026-02-04 17:40:44'),(17,12,'Residents','Updated','Profile updated',1,'2026-02-04 17:40:48'),(18,12,'Residents','Updated','Profile updated',1,'2026-02-04 17:52:07'),(19,12,'Residents','Updated','Profile updated',1,'2026-02-04 17:52:10'),(20,12,'Residents','Updated','Profile updated',1,'2026-02-04 17:52:11'),(21,22,'Residents','Created','daryll Velonio',1,'2026-02-04 17:59:36'),(22,22,'Residents','Updated','Profile updated',1,'2026-02-04 19:32:28'),(23,12,'Residents','Updated','Profile updated',1,'2026-02-04 19:32:29'),(24,12,'Residents','Updated','Profile updated',1,'2026-02-04 19:32:31'),(25,12,'Residents','Updated','Profile updated',1,'2026-02-04 19:32:32'),(26,12,'Residents','Updated','Profile updated',1,'2026-02-05 01:05:30'),(27,12,'Residents','Updated','Profile updated',1,'2026-02-05 01:50:41'),(28,12,'Residents','Updated','Profile updated',1,'2026-02-05 02:07:29'),(29,16,'Residents','Deleted','Pedro Santos',1,'2026-02-05 02:07:39'),(30,12,'Residents','Updated','Profile updated',1,'2026-02-05 19:14:49'),(31,12,'Residents','Updated','Profile updated',1,'2026-02-05 19:47:05'),(32,13,'Residents','Updated','Profile updated',1,'2026-02-05 20:34:17'),(33,12,'Residents','Updated','Profile updated',1,'2026-02-05 22:50:58'),(34,12,'Residents','Updated','Profile updated',1,'2026-02-07 12:42:37'),(35,12,'Blotter','Filed','daw - Ana Lopez Ramos',1,'2026-02-07 14:57:31'),(36,12,'Blotter','Filed','child abuse - daryll C Velonio',1,'2026-02-07 14:57:58'),(37,22,'Residents','Updated','Profile updated',1,'2026-02-08 20:58:04'),(38,13,'Residents','Updated','Profile updated',1,'2026-02-11 14:16:44'),(39,13,'Residents','Updated','Profile updated',1,'2026-02-11 14:17:46'),(40,13,'Residents','Updated','Profile updated',1,'2026-02-11 14:17:55'),(41,12,'AUTH','REGISTER','User admin registered with role Admin',7,'2026-02-18 16:19:39'),(42,12,'AUTH','LOGIN','User admin signed in',7,'2026-02-18 16:19:45'),(43,12,'AUTH','LOGIN','User admin signed in',7,'2026-02-18 16:47:13'),(44,12,'Residents','Updated','Profile updated',3,'2026-02-18 18:49:26'),(45,12,'Residents','Updated','Profile updated',3,'2026-02-20 15:12:07'),(46,12,'Residents','Updated','Profile updated',3,'2026-02-20 15:12:10'),(47,12,'Residents','Updated','Profile updated',3,'2026-02-20 16:35:26'),(48,12,'Residents','Transferred','From: 001 Rizal Street, Purok 1 | To: Default Purok',3,'2026-02-20 16:35:26'),(49,12,'Residents','Updated','Profile updated via Resident Details modal',3,'2026-02-20 18:55:27'),(50,12,'Residents','Updated','Profile updated via Resident Details modal',3,'2026-02-20 19:16:39'),(51,12,'Residents','Updated','Profile updated via Resident Details modal',3,'2026-02-21 11:24:23');
/*!40000 ALTER TABLE `activity_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:18
