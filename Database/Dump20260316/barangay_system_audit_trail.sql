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
-- Table structure for table `audit_trail`
--

DROP TABLE IF EXISTS `audit_trail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `audit_trail` (
  `audit_id` bigint NOT NULL AUTO_INCREMENT,
  `module` varchar(60) NOT NULL,
  `entity_type` varchar(60) NOT NULL,
  `entity_id` varchar(64) DEFAULT NULL,
  `action` varchar(60) NOT NULL,
  `before_json` longtext,
  `after_json` longtext,
  `notes` text,
  `action_by` int DEFAULT NULL,
  `action_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`audit_id`),
  KEY `idx_audit_entity` (`entity_type`,`entity_id`),
  KEY `idx_audit_module` (`module`),
  KEY `idx_audit_action_at` (`action_at`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `audit_trail`
--

LOCK TABLES `audit_trail` WRITE;
/*!40000 ALTER TABLE `audit_trail` DISABLE KEYS */;
INSERT INTO `audit_trail` VALUES (1,'Users','user_account','7','UPDATE','{\"UserId\":7,\"Username\":\"admin\",\"FirstName\":\"\",\"MiddleName\":\"\",\"LastName\":\"\",\"FullName\":\"admin\",\"Email\":\"\",\"ContactNo\":\"\",\"Position\":\"\",\"Department\":\"\",\"LastProject\":\"\",\"IsActive\":true,\"PhotoUrl\":\"\",\"Role\":\"Admin\"}','{\"UserId\":7,\"Username\":\"admin\",\"FirstName\":\"\",\"MiddleName\":\"\",\"LastName\":\"\",\"FullName\":\"\",\"Email\":\"\",\"ContactNo\":\"\",\"Position\":\"\",\"Department\":\"\",\"LastProject\":\"\",\"IsActive\":true,\"PhotoUrl\":\"C:\\\\Users\\\\Loona\\\\Downloads\\\\136bb18c-aa50-4589-b21c-6bfd6739c050.jpg\",\"Role\":\"Admin\"}','User account updated.',3,'2026-02-18 18:27:41'),(2,'Residents','resident','12','UPDATE','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','Profile updated',3,'2026-02-18 18:49:26'),(3,'Residents','resident','12','UPDATE','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','Profile updated',3,'2026-02-20 15:12:07'),(4,'Residents','resident','12','UPDATE','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','Profile updated',3,'2026-02-20 15:12:10'),(5,'Residents','resident','12','UPDATE','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":2,\"HouseholdId\":1,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','{\"ResidentId\":12,\"BarangayId\":1,\"PurokId\":1,\"HouseholdId\":null,\"FirstName\":\"Juan\",\"MiddleName\":\"Dela\",\"LastName\":\"Cruz\",\"Sex\":\"M\",\"BirthDate\":\"1985-03-15T00:00:00\",\"CivilStatus\":\"Married\",\"ContactNo\":\"09171234567\",\"Status\":\"ACTIVE\",\"IsDeleted\":false,\"DeletedAt\":null,\"DeletedByUserId\":null,\"DeleteReason\":\"\"}','Profile updated',3,'2026-02-20 16:35:26'),(6,'Blotter','case_hearing','1','SCHEDULE',NULL,'{\"HearingId\":1,\"CaseId\":2,\"ScheduleAt\":\"2026-02-22T09:00:00\",\"Venue\":\"lower bala\",\"Status\":\"SCHEDULED\"}','Mediation scheduled.',3,'2026-02-21 19:01:10');
/*!40000 ALTER TABLE `audit_trail` ENABLE KEYS */;
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
