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
-- Table structure for table `case_record`
--

DROP TABLE IF EXISTS `case_record`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `case_record` (
  `case_id` int NOT NULL AUTO_INCREMENT,
  `barangay_id` int NOT NULL,
  `case_type_id` int NOT NULL,
  `case_no` varchar(50) DEFAULT NULL,
  `date_filed` date DEFAULT NULL,
  `incident_date` date DEFAULT NULL,
  `incident_location` varchar(255) DEFAULT NULL,
  `summary` text,
  `status` enum('OPEN','ONGOING','SETTLED','REFERRED','CLOSED') DEFAULT 'OPEN',
  `handled_by_user_id` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `complainant_id` int DEFAULT NULL,
  `respondent_resident_id` int DEFAULT NULL,
  `respondent_name` varchar(255) DEFAULT NULL,
  `incident_type` varchar(100) DEFAULT NULL,
  `incident_time` time DEFAULT NULL,
  `witness_names` text,
  `action_taken` text,
  `resolution_details` text,
  `incident_details` text,
  `recorded_by` int DEFAULT NULL,
  `ai_summary` text,
  `ai_key_points` text,
  `ai_category` varchar(150) DEFAULT NULL,
  `ai_category_confidence` decimal(5,4) DEFAULT NULL,
  `ai_risk_level` varchar(20) DEFAULT NULL,
  `ai_risk_score` int DEFAULT NULL,
  `ai_risk_reasons` text,
  `ai_entities` text,
  `ai_recommended_next_action` text,
  `ai_model` varchar(100) DEFAULT NULL,
  `ai_processed_at` datetime DEFAULT NULL,
  `referral_destination` varchar(255) DEFAULT NULL,
  `closure_notes` text,
  `closed_at` datetime DEFAULT NULL,
  `closed_by_user_id` int DEFAULT NULL,
  PRIMARY KEY (`case_id`),
  KEY `case_type_id` (`case_type_id`),
  KEY `handled_by_user_id` (`handled_by_user_id`),
  KEY `idx_case_record_barangay` (`barangay_id`),
  KEY `idx_case_record_status` (`status`),
  KEY `idx_case_record_incident_date` (`incident_date`),
  KEY `idx_case_record_date_status` (`date_filed`,`status`,`complainant_id`),
  CONSTRAINT `case_record_ibfk_1` FOREIGN KEY (`barangay_id`) REFERENCES `barangay` (`barangay_id`) ON DELETE CASCADE,
  CONSTRAINT `case_record_ibfk_2` FOREIGN KEY (`case_type_id`) REFERENCES `case_type` (`case_type_id`),
  CONSTRAINT `case_record_ibfk_3` FOREIGN KEY (`handled_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `case_record`
--

LOCK TABLES `case_record` WRITE;
/*!40000 ALTER TABLE `case_record` DISABLE KEYS */;
INSERT INTO `case_record` VALUES (1,1,1,NULL,'2026-02-07','2026-02-07',NULL,'child','ONGOING',1,'2026-02-07 06:57:31','2026-02-11 14:06:23',12,NULL,'Ana Lopez Ramos','daw',NULL,NULL,NULL,NULL,'child',1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL),(2,1,1,NULL,'2026-02-07','2026-02-07',NULL,'my child is aused','ONGOING',1,'2026-02-07 06:57:58','2026-02-11 14:06:23',12,NULL,'daryll C Velonio','thief',NULL,NULL,NULL,NULL,'my child is aused',1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL);
/*!40000 ALTER TABLE `case_record` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:26
