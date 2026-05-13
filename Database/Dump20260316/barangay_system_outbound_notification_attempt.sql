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
-- Table structure for table `outbound_notification_attempt`
--

DROP TABLE IF EXISTS `outbound_notification_attempt`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `outbound_notification_attempt` (
  `attempt_id` bigint NOT NULL AUTO_INCREMENT,
  `notification_id` bigint NOT NULL,
  `attempt_no` int NOT NULL,
  `attempted_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `success` tinyint(1) NOT NULL DEFAULT '0',
  `response_code` varchar(64) DEFAULT NULL,
  `response_message` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`attempt_id`),
  KEY `idx_notification_attempt_notification` (`notification_id`,`attempted_at`),
  CONSTRAINT `fk_notification_attempt_notification` FOREIGN KEY (`notification_id`) REFERENCES `outbound_notification` (`notification_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `outbound_notification_attempt`
--

LOCK TABLES `outbound_notification_attempt` WRITE;
/*!40000 ALTER TABLE `outbound_notification_attempt` DISABLE KEYS */;
INSERT INTO `outbound_notification_attempt` VALUES (1,1,1,'2026-02-19 12:37:18',0,'SKIPPED','SMS API not configured.'),(2,2,1,'2026-02-19 12:37:18',0,'SKIPPED','SMS API not configured.'),(3,3,1,'2026-02-20 15:09:12',0,'SKIPPED','SMS API not configured.'),(4,4,1,'2026-02-20 15:09:12',0,'SKIPPED','SMS API not configured.'),(5,27,1,'2026-02-21 11:14:20',0,'SKIPPED','SMS API not configured.'),(6,28,1,'2026-02-21 11:14:20',0,'SKIPPED','SMS API not configured.'),(7,31,1,'2026-02-21 15:15:42',0,'SKIPPED','SMTP not configured.'),(8,33,1,'2026-02-21 15:15:42',0,'SKIPPED','SMTP not configured.'),(9,79,1,'2026-02-22 13:06:46',0,'SKIPPED','SMTP not configured.'),(10,80,1,'2026-02-22 13:06:46',0,'SKIPPED','SMS API not configured.'),(11,81,1,'2026-02-22 13:06:46',0,'SKIPPED','SMTP not configured.'),(12,82,1,'2026-02-22 13:06:46',0,'SKIPPED','SMS API not configured.'),(13,91,1,'2026-02-23 17:33:23',0,'SKIPPED','SMTP not configured.'),(14,92,1,'2026-02-23 17:33:23',0,'SKIPPED','SMS API not configured.'),(15,93,1,'2026-02-23 17:33:23',0,'SKIPPED','SMTP not configured.'),(16,94,1,'2026-02-23 17:33:23',0,'SKIPPED','SMS API not configured.'),(17,95,1,'2026-02-27 19:43:14',0,'SKIPPED','SMTP not configured.'),(18,96,1,'2026-02-27 19:43:14',0,'SKIPPED','SMS API not configured.'),(19,97,1,'2026-02-27 19:43:14',0,'SKIPPED','SMTP not configured.'),(20,98,1,'2026-02-27 19:43:14',0,'SKIPPED','SMS API not configured.'),(21,99,1,'2026-03-02 10:45:20',0,'SKIPPED','SMTP not configured.'),(22,100,1,'2026-03-02 10:45:20',0,'SKIPPED','SMS API not configured.'),(23,101,1,'2026-03-02 10:45:20',0,'SKIPPED','SMTP not configured.'),(24,102,1,'2026-03-02 10:45:20',0,'SKIPPED','SMS API not configured.'),(25,103,1,'2026-03-04 17:26:02',0,'SKIPPED','SMTP not configured.'),(26,104,1,'2026-03-04 17:26:02',0,'SKIPPED','SMS API not configured.'),(27,105,1,'2026-03-04 17:26:02',0,'SKIPPED','SMTP not configured.'),(28,106,1,'2026-03-04 17:26:02',0,'SKIPPED','SMS API not configured.'),(29,107,1,'2026-03-05 09:24:03',0,'SKIPPED','SMTP not configured.'),(30,108,1,'2026-03-05 09:24:03',0,'SKIPPED','SMS API not configured.'),(31,109,1,'2026-03-05 09:24:03',0,'SKIPPED','SMTP not configured.'),(32,110,1,'2026-03-05 09:24:03',0,'SKIPPED','SMS API not configured.'),(33,111,1,'2026-03-06 12:22:46',0,'SKIPPED','SMTP not configured.'),(34,112,1,'2026-03-06 12:22:46',0,'SKIPPED','SMS API not configured.'),(35,113,1,'2026-03-06 12:22:46',0,'SKIPPED','SMTP not configured.'),(36,114,1,'2026-03-06 12:22:46',0,'SKIPPED','SMS API not configured.');
/*!40000 ALTER TABLE `outbound_notification_attempt` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:22
