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
-- Table structure for table `purok_sitio`
--

DROP TABLE IF EXISTS `purok_sitio`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `purok_sitio` (
  `purok_id` int NOT NULL AUTO_INCREMENT,
  `barangay_id` int NOT NULL,
  `name` varchar(150) NOT NULL,
  `type` enum('PUROK','SITIO') DEFAULT 'PUROK',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `latitude` decimal(10,8) DEFAULT NULL,
  `longitude` decimal(11,8) DEFAULT NULL,
  PRIMARY KEY (`purok_id`),
  KEY `idx_purok_barangay` (`barangay_id`),
  KEY `idx_purok_coordinates` (`latitude`,`longitude`),
  CONSTRAINT `purok_sitio_ibfk_1` FOREIGN KEY (`barangay_id`) REFERENCES `barangay` (`barangay_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `purok_sitio`
--

LOCK TABLES `purok_sitio` WRITE;
/*!40000 ALTER TABLE `purok_sitio` DISABLE KEYS */;
INSERT INTO `purok_sitio` VALUES (1,1,'Default Purok','PUROK','2026-02-11 12:42:26','2026-02-11 12:42:26',NULL,NULL),(2,1,'Purok 1','PUROK','2026-02-11 14:08:30','2026-02-11 14:08:30',NULL,NULL),(4,1,'Purok 2','PUROK','2026-02-11 14:08:30','2026-02-11 14:08:30',NULL,NULL),(6,1,'Purok 3','PUROK','2026-02-11 14:08:30','2026-02-11 14:08:30',NULL,NULL),(7,1,'Purok 1','PUROK','2026-02-16 04:59:05','2026-02-16 04:59:05',NULL,NULL),(8,1,'Purok 1','PUROK','2026-02-16 04:59:05','2026-02-16 04:59:05',NULL,NULL),(9,1,'Purok 2','PUROK','2026-02-16 04:59:05','2026-02-16 04:59:05',NULL,NULL),(10,1,'Purok 2','PUROK','2026-02-16 04:59:05','2026-02-16 04:59:05',NULL,NULL),(11,1,'Purok 3','PUROK','2026-02-16 04:59:05','2026-02-16 04:59:05',NULL,NULL);
/*!40000 ALTER TABLE `purok_sitio` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:25
