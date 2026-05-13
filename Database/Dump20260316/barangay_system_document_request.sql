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
-- Table structure for table `document_request`
--

DROP TABLE IF EXISTS `document_request`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `document_request` (
  `doc_request_id` int NOT NULL AUTO_INCREMENT,
  `barangay_id` int NOT NULL,
  `doc_type_id` int NOT NULL,
  `resident_id` int NOT NULL,
  `purpose` varchar(255) DEFAULT NULL,
  `status` enum('DRAFT','SUBMITTED','APPROVED','RELEASED','REJECTED','CANCELLED') DEFAULT 'SUBMITTED',
  `requested_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `approved_at` datetime DEFAULT NULL,
  `released_at` datetime DEFAULT NULL,
  `requested_by_user_id` int DEFAULT NULL,
  `approved_by_user_id` int DEFAULT NULL,
  `released_by_user_id` int DEFAULT NULL,
  `remarks` text,
  `document_no` varchar(50) DEFAULT NULL,
  `fee` decimal(10,2) DEFAULT '0.00',
  `or_number` varchar(100) DEFAULT NULL,
  `business_name` varchar(255) DEFAULT NULL,
  `business_nature` varchar(255) DEFAULT NULL,
  `print_count` int NOT NULL DEFAULT '0',
  `last_printed_at` datetime DEFAULT NULL,
  `verification_token` varchar(32) DEFAULT NULL,
  `verification_token_created_at` datetime DEFAULT NULL,
  `expires_at` datetime DEFAULT NULL,
  `renewed_from_request_id` int DEFAULT NULL,
  `renewal_notified_at` datetime DEFAULT NULL,
  `release_notified_at` datetime DEFAULT NULL,
  PRIMARY KEY (`doc_request_id`),
  UNIQUE KEY `ux_document_request_verification_token` (`verification_token`),
  KEY `barangay_id` (`barangay_id`),
  KEY `doc_type_id` (`doc_type_id`),
  KEY `requested_by_user_id` (`requested_by_user_id`),
  KEY `approved_by_user_id` (`approved_by_user_id`),
  KEY `released_by_user_id` (`released_by_user_id`),
  KEY `idx_doc_request_resident` (`resident_id`),
  KEY `idx_doc_request_status` (`status`),
  KEY `idx_doc_request_requested_at` (`requested_at`),
  KEY `idx_doc_request_resident_status` (`resident_id`,`status`),
  KEY `idx_document_request_expires_at` (`expires_at`),
  KEY `idx_document_request_renewed_from` (`renewed_from_request_id`),
  CONSTRAINT `document_request_ibfk_1` FOREIGN KEY (`barangay_id`) REFERENCES `barangay` (`barangay_id`) ON DELETE CASCADE,
  CONSTRAINT `document_request_ibfk_2` FOREIGN KEY (`doc_type_id`) REFERENCES `document_type` (`doc_type_id`),
  CONSTRAINT `document_request_ibfk_3` FOREIGN KEY (`resident_id`) REFERENCES `resident` (`resident_id`),
  CONSTRAINT `document_request_ibfk_4` FOREIGN KEY (`requested_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `document_request_ibfk_5` FOREIGN KEY (`approved_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `document_request_ibfk_6` FOREIGN KEY (`released_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `document_request`
--

LOCK TABLES `document_request` WRITE;
/*!40000 ALTER TABLE `document_request` DISABLE KEYS */;
INSERT INTO `document_request` VALUES (1,1,1,12,'baranggay clearance','APPROVED','2026-02-04 01:51:14','2026-02-04 02:02:01','2026-02-04 00:00:00',NULL,1,1,NULL,'#1',150.00,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL);
/*!40000 ALTER TABLE `document_request` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-16 12:33:19
