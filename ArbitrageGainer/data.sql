CREATE DATABASE  IF NOT EXISTS `team_database_schema` /*!40100 DEFAULT CHARACTER SET utf8mb3 */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `team_database_schema`;
-- MySQL dump 10.13  Distrib 8.0.36, for macos14 (arm64)
--
-- Host: cmu-fp.mysql.database.azure.com    Database: team_database_schema
-- ------------------------------------------------------
-- Server version	8.0.36

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
-- Table structure for table `cross_traded_pairs`
--

DROP TABLE IF EXISTS `cross_traded_pairs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cross_traded_pairs` (
  `BaseCurrency` varchar(255) NOT NULL,
  `QuoteCurrency` varchar(255) NOT NULL,
  PRIMARY KEY (`BaseCurrency`,`QuoteCurrency`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cross_traded_pairs`
--

LOCK TABLES `cross_traded_pairs` WRITE;
/*!40000 ALTER TABLE `cross_traded_pairs` DISABLE KEYS */;
INSERT INTO `cross_traded_pairs` VALUES ('1INCH','USD'),('ADA','USD'),('APE','USD'),('AXS','USD'),('BAND','USD'),('BAT','USD'),('BLUR','USD'),('CHZ','USD'),('COMP','USD'),('CRV','USD'),('DAI','USD'),('DOT','USD'),('ETH','EUR'),('ETH','GBP'),('ETH','USD'),('EUR','USD'),('FET','USD'),('FLR','USD'),('FTM','USD'),('GALA','USD'),('GBP','USD'),('GRT','USD'),('INJ','USD'),('KNC','USD'),('LDO','USD'),('LINK','USD'),('LRC','USD'),('LTC','USD'),('MATIC','USD'),('MKR','USD'),('NEAR','USD'),('PEPE','USD'),('SAND','USD'),('SGB','USD'),('SHIB','USD'),('SNX','USD'),('SOL','USD'),('SUI','USD'),('SUSHI','USD'),('UNI','USD'),('USD','USD'),('XLM','USD'),('XRP','USD'),('YFI','USD'),('ZRX','USD');
/*!40000 ALTER TABLE `cross_traded_pairs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `historical_spread`
--

DROP TABLE IF EXISTS `historical_spread`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `historical_spread` (
  `my_row_id` bigint unsigned NOT NULL AUTO_INCREMENT /*!80023 INVISIBLE */,
  `Pair` varchar(255) DEFAULT NULL,
  `NumberOfOpportunities` int DEFAULT NULL,
  PRIMARY KEY (`my_row_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `historical_spread`
--

LOCK TABLES `historical_spread` WRITE;
/*!40000 ALTER TABLE `historical_spread` DISABLE KEYS */;
INSERT INTO `historical_spread` (`my_row_id`, `Pair`, `NumberOfOpportunities`) VALUES (1,'MKR-USD',34),(2,'FET-USD',5),(3,'SOL-USD',3),(4,'DOT-USD',2);
/*!40000 ALTER TABLE `historical_spread` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transactions`
--

DROP TABLE IF EXISTS `transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `transactions` (
  `TransactionID` int NOT NULL AUTO_INCREMENT,
  `TransactionType` varchar(255) DEFAULT NULL,
  `Price` decimal(10,2) DEFAULT NULL,
  `Amount` decimal(10,2) DEFAULT NULL,
  `TransactionDate` datetime DEFAULT NULL,
  PRIMARY KEY (`TransactionID`)
) ENGINE=InnoDB AUTO_INCREMENT=123 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transactions`
--

LOCK TABLES `transactions` WRITE;
/*!40000 ALTER TABLE `transactions` DISABLE KEYS */;
INSERT INTO `transactions` VALUES (1,'Buy',33000.00,1.00,'2024-04-14 12:45:43'),(2,'Sell',34000.00,1.00,'2024-04-14 12:45:44'),(3,'Buy',2500.00,5.00,'2024-04-14 12:45:44'),(4,'Sell',2600.00,5.00,'2024-04-14 12:45:44'),(5,'Buy',140.00,10.00,'2024-04-14 12:45:44'),(6,'Sell',150.00,10.00,'2024-04-14 12:45:44'),(7,'Buy',33000.00,1.00,'2024-04-14 12:51:46'),(8,'Sell',34000.00,1.00,'2024-04-14 12:51:47'),(9,'Buy',2500.00,5.00,'2024-04-14 12:51:47'),(10,'Sell',2600.00,5.00,'2024-04-14 12:51:47'),(11,'Buy',140.00,10.00,'2024-04-14 12:51:47'),(12,'Sell',150.00,10.00,'2024-04-14 12:51:47'),(13,'Buy',33000.00,1.00,'2024-04-14 12:51:47'),(14,'Sell',35000.00,1.00,'2024-04-14 12:51:47'),(15,'Buy',33000.00,1.00,'2024-04-14 12:55:16'),(16,'Sell',34000.00,1.00,'2024-04-14 12:55:16'),(17,'Buy',2500.00,5.00,'2024-04-14 12:55:16'),(18,'Sell',2600.00,5.00,'2024-04-14 12:55:16'),(19,'Buy',140.00,10.00,'2024-04-14 12:55:16'),(20,'Sell',150.00,10.00,'2024-04-14 12:55:17'),(21,'Buy',33000.00,1.00,'2024-04-14 12:55:17'),(22,'Sell',35000.00,1.00,'2024-04-14 12:55:17'),(23,'Buy',33000.00,1.00,'2024-04-14 12:55:56'),(24,'Sell',34000.00,1.00,'2024-04-14 12:55:57'),(25,'Buy',2500.00,5.00,'2024-04-14 12:55:57'),(26,'Sell',2600.00,5.00,'2024-04-14 12:55:57'),(27,'Buy',140.00,10.00,'2024-04-14 12:55:57'),(28,'Sell',150.00,10.00,'2024-04-14 12:55:57'),(29,'Buy',33000.00,1.00,'2024-04-14 12:55:57'),(30,'Sell',33050.00,1.00,'2024-04-14 12:55:57'),(31,'Buy',33000.00,1.00,'2024-04-14 12:57:57'),(32,'Sell',34000.00,1.00,'2024-04-14 12:57:58'),(33,'Buy',2500.00,5.00,'2024-04-14 12:57:58'),(34,'Sell',2600.00,5.00,'2024-04-14 12:57:58'),(35,'Buy',140.00,10.00,'2024-04-14 12:57:58'),(36,'Sell',150.00,10.00,'2024-04-14 12:57:58'),(37,'Buy',33000.00,1.00,'2024-04-14 12:57:58'),(38,'Sell',33050.00,1.00,'2024-04-14 12:57:58'),(39,'Buy',33000.00,1.00,'2024-04-14 12:58:36'),(40,'Sell',34000.00,1.00,'2024-04-14 12:58:37'),(41,'Buy',2500.00,5.00,'2024-04-14 12:58:37'),(42,'Sell',2600.00,5.00,'2024-04-14 12:58:37'),(43,'Buy',140.00,10.00,'2024-04-14 12:58:37'),(44,'Sell',150.00,10.00,'2024-04-14 12:58:37'),(45,'Buy',33000.00,1.00,'2024-04-14 12:58:37'),(46,'Sell',33050.00,1.00,'2024-04-14 12:58:37'),(47,'Buy',33000.00,1.00,'2024-04-14 13:00:13'),(48,'Sell',34000.00,1.00,'2024-04-14 13:00:14'),(49,'Buy',2500.00,5.00,'2024-04-14 13:00:14'),(50,'Sell',2600.00,5.00,'2024-04-14 13:00:14'),(51,'Buy',140.00,10.00,'2024-04-14 13:00:15'),(52,'Sell',150.00,10.00,'2024-04-14 13:00:15'),(53,'Buy',33000.00,1.00,'2024-04-14 13:00:15'),(54,'Sell',35000.00,1.00,'2024-04-14 13:00:15'),(55,'Buy',33000.00,1.00,'2024-04-14 13:03:19'),(56,'Sell',34000.00,1.00,'2024-04-14 13:03:20'),(57,'Buy',2500.00,5.00,'2024-04-14 13:03:20'),(58,'Sell',2600.00,5.00,'2024-04-14 13:03:20'),(59,'Buy',140.00,10.00,'2024-04-14 13:03:20'),(60,'Sell',150.00,10.00,'2024-04-14 13:03:20'),(61,'Buy',33000.00,1.00,'2024-04-14 13:03:20'),(62,'Sell',35000.00,1.00,'2024-04-14 13:03:20'),(63,'Buy',33000.00,1.00,'2024-04-14 13:05:21'),(64,'Sell',34000.00,1.00,'2024-04-14 13:05:22'),(65,'Buy',2500.00,5.00,'2024-04-14 13:05:24'),(66,'Sell',2600.00,5.00,'2024-04-14 13:05:24'),(67,'Buy',140.00,10.00,'2024-04-14 13:05:24'),(68,'Sell',150.00,10.00,'2024-04-14 13:05:24'),(69,'Buy',33000.00,1.00,'2024-04-14 13:05:24'),(70,'Sell',35000.00,1.00,'2024-04-14 13:05:24'),(71,'Buy',33000.00,1.00,'2024-04-14 13:10:22'),(72,'Sell',34000.00,1.00,'2024-04-14 13:10:22'),(73,'Buy',2500.00,5.00,'2024-04-14 13:10:24'),(74,'Sell',2600.00,5.00,'2024-04-14 13:10:24'),(75,'Buy',140.00,10.00,'2024-04-14 13:10:24'),(76,'Sell',150.00,10.00,'2024-04-14 13:10:24'),(77,'Buy',33000.00,1.00,'2024-04-14 13:10:24'),(78,'Sell',35000.00,1.00,'2024-04-14 13:10:24'),(79,'Buy',33000.00,1.00,'2024-04-15 00:30:54'),(80,'Sell',34000.00,1.00,'2024-04-15 00:30:55'),(81,'Buy',33000.00,1.00,'2024-04-15 00:32:17'),(82,'Sell',34000.00,1.00,'2024-04-15 00:32:18'),(83,'Buy',2500.00,5.00,'2024-04-15 00:32:21'),(84,'Sell',2600.00,5.00,'2024-04-15 00:32:22'),(85,'Buy',140.00,10.00,'2024-04-15 00:32:22'),(86,'Sell',150.00,10.00,'2024-04-15 00:32:22'),(87,'Buy',33000.00,1.00,'2024-04-15 00:32:22'),(88,'Sell',35000.00,1.00,'2024-04-15 00:32:22'),(89,'Buy',33000.00,1.00,'2024-04-15 00:38:40'),(90,'Sell',34000.00,1.00,'2024-04-15 00:38:41'),(91,'Buy',33000.00,1.00,'2024-04-28 00:36:57'),(92,'Sell',34000.00,1.00,'2024-04-28 00:36:58'),(93,'Buy',0.00,0.00,'2024-04-28 06:40:07'),(94,'Buy',0.00,0.00,'2024-04-28 07:16:59'),(95,'Buy',0.00,0.00,'2024-04-28 07:17:55'),(96,'Buy',0.00,0.00,'2024-04-28 07:22:41'),(97,'Buy',0.00,0.00,'2024-04-28 07:30:55'),(98,'Buy',0.00,0.00,'2024-04-28 07:31:00'),(99,'Sell',0.00,0.00,'2024-04-28 07:31:09'),(100,'Buy',0.00,0.00,'2024-04-28 08:19:40'),(101,'Buy',0.00,0.00,'2024-04-28 08:19:44'),(102,'Sell',0.00,0.00,'2024-04-28 08:19:53'),(103,'Buy',0.00,0.00,'2024-04-28 09:47:54'),(104,'Buy',0.00,0.00,'2024-04-28 09:48:02'),(105,'Sell',0.00,0.00,'2024-04-28 09:48:10'),(106,'Buy',0.00,0.00,'2024-04-28 09:56:05'),(107,'Buy',28.06,35.00,'2024-04-28 22:56:07'),(108,'Buy',28.06,35.00,'2024-04-28 23:05:33'),(109,'Buy',22.45,58.06,'2024-04-28 23:25:35'),(110,'Buy',22.45,58.06,'2024-04-28 23:32:48'),(111,'Buy',28.06,35.00,'2024-04-28 23:36:31'),(112,'Buy',28.06,35.00,'2024-04-28 23:43:09'),(113,'Sell',28.22,22.45,'2024-04-28 23:43:15'),(114,'Sell',28.22,12.55,'2024-04-28 23:43:15'),(115,'Buy',28.06,35.00,'2024-04-28 23:47:13'),(116,'Sell',28.22,35.00,'2024-04-28 23:47:19'),(117,'Buy',28.06,35.00,'2024-04-28 23:50:26'),(118,'Sell',28.22,35.00,'2024-04-28 23:50:31'),(119,'Buy',28.06,35.00,'2024-04-28 23:52:37'),(120,'Sell',28.22,35.00,'2024-04-28 23:52:42'),(121,'Buy',28.06,35.00,'2024-04-28 23:55:14'),(122,'Sell',28.22,35.00,'2024-04-28 23:55:20');
/*!40000 ALTER TABLE `transactions` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2024-04-28 17:06:31
