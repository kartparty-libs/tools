SET FOREIGN_KEY_CHECKS=0;

CREATE TABLE IF NOT EXISTS `globalinfo` (
  `serverid` int(11) NOT NULL,
  `opentime` bigint(20) NOT NULL DEFAULT '0' COMMENT '服务器创建时间戳',
  `zerotime` bigint(20) NOT NULL DEFAULT '0' COMMENT '今日凌晨时间',
  `playerindex` int(11) NOT NULL DEFAULT '0'COMMENT '玩家id自增',
  PRIMARY KEY (`serverid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*----------------玩家相关表----------------*/

CREATE TABLE IF NOT EXISTS `player` (
  `account` varchar(128) NOT NULL,
  `playerinstid` bigint(20) NOT NULL,
  `playername` char(32) NOT NULL COMMENT '角色名',
  `password` varchar(128) NOT NULL COMMENT '密码',
  `createtime` bigint(20) NOT NULL DEFAULT '0' COMMENT '创建时间戳',
  PRIMARY KEY (`playerinstid`),
  KEY `player_account_index` (`account`) USING BTREE,
  KEY `player_playerinstid_index` (`playerinstid`) USING BTREE,
  KEY `player_playername_index` (`playername`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*----------------管理器相关表----------------*/


/*----------------后续添加字段----------------*/
/*
ALTER TABLE `player_energyinfo`  
ADD COLUMN `isinitenergy` smallint(6) NOT NULL DEFAULT '0' COMMENT '是否初始化了体力';
*/