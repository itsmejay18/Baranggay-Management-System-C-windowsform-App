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
-- Table structure for table `resident_transfer_history`
--

DROP TABLE IF EXISTS `resident_transfer_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `resident_transfer_history` (
  `transfer_id` bigint NOT NULL AUTO_INCREMENT,
  `resident_id` int NOT NULL,
  `old_purok_id` int DEFAULT NULL,
  `old_household_id` int DEFAULT NULL,
  `old_address` varchar(255) DEFAULT NULL,
  `new_purok_id` int DEFAULT NULL,
  `new_household_id` int DEFAULT NULL,
  `new_address` varchar(255) DEFAULT NULL,
  `transfer_reason` varchar(255) DEFAULT NULL,
  `transferred_by_user_id` int DEFAULT NULL,
  `transferred_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`transfer_id`),
  KEY `idx_transfer_history_resident` (`resident_id`,`transferred_at`),
  KEY `idx_transfer_history_old_location` (`old_purok_id`,`old_household_id`),
  KEY `idx_transfer_history_new_location` (`new_purok_id`,`new_household_id`),
  KEY `fk_transfer_history_old_household` (`old_household_id`),
  KEY `fk_transfer_history_new_household` (`new_household_id`),
  KEY `fk_transfer_history_user` (`transferred_by_user_id`),
  CONSTRAINT `fk_transfer_history_new_household` FOREIGN KEY (`new_household_id`) REFERENCES `household` (`household_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_transfer_history_new_purok` FOREIGN KEY (`new_purok_id`) REFERENCES `purok_sitio` (`purok_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_transfer_history_old_household` FOREIGN KEY (`old_household_id`) REFERENCES `household` (`household_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_transfer_history_old_purok` FOREIGN KEY (`old_purok_id`) REFERENCES `purok_sitio` (`purok_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_transfer_history_resident` FOREIGN KEY (`resident_id`) REFERENCES `resident` (`resident_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_transfer_history_user` FOREIGN KEY (`transferred_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `resident_transfer_history`
--

LOCK TABLES `resident_transfer_history` WRITE;
/*!40000 ALTER TABLE `resident_transfer_history` DISABLE KEYS */;
INSERT INTO `resident_transfer_history` VALUES (1,12,2,1,'001 Rizal Street, Purok 1',1,NULL,'Default Purok','Profile location updated',3,'2026-02-20 16:35:26');
/*!40000 ALTER TABLE `resident_transfer_history` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:28
