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
-- Table structure for table `backup_run`
--

DROP TABLE IF EXISTS `backup_run`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `backup_run` (
  `backup_run_id` int NOT NULL AUTO_INCREMENT,
  `started_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ended_at` datetime DEFAULT NULL,
  `status` enum('RUNNING','SUCCESS','FAILED') NOT NULL DEFAULT 'RUNNING',
  `file_path` varchar(500) DEFAULT NULL,
  `file_size_bytes` bigint DEFAULT NULL,
  `error_message` text,
  `created_by_user_id` int DEFAULT NULL,
  PRIMARY KEY (`backup_run_id`),
  KEY `idx_backup_run_started_at` (`started_at`),
  KEY `idx_backup_run_status` (`status`),
  KEY `created_by_user_id` (`created_by_user_id`),
  CONSTRAINT `backup_run_ibfk_1` FOREIGN KEY (`created_by_user_id`) REFERENCES `user_account` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `backup_run`
--

LOCK TABLES `backup_run` WRITE;
/*!40000 ALTER TABLE `backup_run` DISABLE KEYS */;
INSERT INTO `backup_run` VALUES (1,'2026-02-16 12:52:25','2026-02-16 12:52:26','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260216-125224.zip',200021,NULL,3),(2,'2026-02-16 23:28:19','2026-02-16 23:28:19','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260216-232819.zip',219967,NULL,3),(3,'2026-02-17 12:25:05','2026-02-17 12:25:07','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260217-122504.zip',227831,NULL,3),(4,'2026-02-18 13:00:55','2026-02-18 13:00:56','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260218-130055.zip',230597,NULL,3),(5,'2026-02-19 12:37:19','2026-02-19 12:37:19','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260219-123718.zip',311579,NULL,3),(6,'2026-02-20 15:09:12','2026-02-20 15:09:13','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260220-150912.zip',312377,NULL,3),(7,'2026-02-21 11:14:21','2026-02-21 11:14:27','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260221-111421.zip',314849,NULL,3),(8,'2026-02-22 13:06:47','2026-02-22 13:06:48','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260222-130646.zip',317127,NULL,3),(9,'2026-02-23 17:33:23','2026-02-23 17:33:25','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260223-173323.zip',317879,NULL,3),(10,'2026-02-27 19:43:14','2026-02-27 19:43:14','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260227-194314.zip',318144,NULL,3),(11,'2026-03-02 10:45:20','2026-03-02 10:45:21','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260302-104519.zip',319098,NULL,3),(12,'2026-03-04 17:26:03','2026-03-04 17:26:04','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260304-172602.zip',320381,NULL,3),(13,'2026-03-05 09:24:04','2026-03-05 09:24:05','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260305-092403.zip',320687,NULL,3),(14,'2026-03-06 12:22:46','2026-03-06 12:22:47','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260306-122245.zip',320874,NULL,3),(15,'2026-03-06 14:14:48','2026-03-06 14:14:49','SUCCESS','C:\\Users\\Loona\\AppData\\Local\\BarangaySystem\\backups\\barangay_system-backup-20260306-141448.zip',321088,NULL,3);
/*!40000 ALTER TABLE `backup_run` ENABLE KEYS */;
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
