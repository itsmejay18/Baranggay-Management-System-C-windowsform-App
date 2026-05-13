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
-- Table structure for table `document_payment`
--

DROP TABLE IF EXISTS `document_payment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `document_payment` (
  `payment_id` int NOT NULL AUTO_INCREMENT,
  `doc_request_id` int NOT NULL,
  `amount` decimal(10,2) DEFAULT NULL,
  `or_no` varchar(100) DEFAULT NULL,
  `payment_method` enum('Cash','GCash','Bank') DEFAULT NULL,
  `paid_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `received_by_user_id` int DEFAULT NULL,
  PRIMARY KEY (`payment_id`),
  UNIQUE KEY `ux_document_payment_or_no` (`or_no`),
  KEY `received_by_user_id` (`received_by_user_id`),
  KEY `idx_doc_payment_request` (`doc_request_id`),
  KEY `idx_doc_payment_paid_at` (`paid_at`),
  CONSTRAINT `document_payment_ibfk_1` FOREIGN KEY (`doc_request_id`) REFERENCES `document_request` (`doc_request_id`) ON DELETE CASCADE,
  CONSTRAINT `document_payment_ibfk_2` FOREIGN KEY (`received_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `document_payment`
--

LOCK TABLES `document_payment` WRITE;
/*!40000 ALTER TABLE `document_payment` DISABLE KEYS */;
/*!40000 ALTER TABLE `document_payment` ENABLE KEYS */;
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
