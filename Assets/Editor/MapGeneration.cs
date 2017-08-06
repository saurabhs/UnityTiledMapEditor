using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.SceneManagement;
using MapGeneration.Utils;

namespace MapGeneration.Core
{
    public class MapGeneration : EditorWindow
    {
        public static string folderNameToAllAssets = @"";
        public static string filenamePrefix = @"";

        [MenuItem( "Map Generation/Generate Map With All Assets" )]
        public static void GenerateMapWithAllAssets()
        {
            var index = 1;
            var worldParent = new GameObject();
            worldParent.name = "_World";

            if ( folderNameToAllAssets.Equals( string.Empty ) || filenamePrefix.Equals( string.Empty ) )
            {
                throw new System.Exception( "Missing folder name / filename to fbx files!" );
            }

            for ( int i = 0; i < Constants.MAP_EDITOR_SHOWCASE_GRID.y; i++ )
            {
                for ( int j = 0; j < Constants.MAP_EDITOR_SHOWCASE_GRID.x; j++ )
                {
                    var filename = folderNameToAllAssets + filenamePrefix + index.ToString();
                    var asset = Resources.Load( filename ) as GameObject;

                    if ( asset == null )
                        continue;

                    asset.transform.localScale = Constants.DEFAULT_LOCAL_SCALE;
                    var levelObject = Instantiate( asset, new Vector3( j * Constants.TILE_SIZE.x, Constants.TILE_SIZE.x, -i * Constants.TILE_SIZE.y ), Quaternion.identity ) as GameObject;
                    levelObject.transform.parent = worldParent.transform;
                    levelObject.name = asset.name;

                    index++;
                }
            }
        }

        [MenuItem( "Map Generation/Generate Map With NavMesh From TMX File" )]
        public static void GenerateMapFromTMXFile()
        {
            var currentScene = SceneManager.GetActiveScene();

            var mapData = ReadTMXFileForMapData( GetMapFilename( currentScene.name ) );
            var worldParent = new GameObject( "_World" );

            var index = 0;
            var tilesetData = mapData.tilesetData;

            for ( var i = 0; i < mapData.height; i++ )
            {
                for ( var j = 0; j < mapData.width; j++ )
                {
                    var id = mapData.data[i][j];
                    var tilesetDataIndex = -1;

                    tilesetDataIndex = tilesetData.FindIndex( obj => id >= obj.firstgid && id <= ( obj.firstgid + obj.tilecount - 1 ) );

                    if ( tilesetDataIndex == -1 )
                        continue;

                    var isRoadPrefab = false;   //required for nav mesh editor flag
                    id -= tilesetData[tilesetDataIndex].firstgid - 1;
                    var prefix = tilesetData[tilesetDataIndex].prefix;
                    var filename = prefix + id.ToString();
                    var scale = new Vector3();  //kenney pack 1 has different scaling compared to kenney pack 2
                    var filepath = GetFilepathForWorld( @"Prefabs/TowerDefense", filename, ref isRoadPrefab, ref scale );

                    if ( filepath.Equals(string.Empty))
                        continue;

                    var asset = Resources.Load( filepath ) as GameObject;
                    if ( asset == null )
                        continue;

                    asset.transform.localScale = scale;

                    var tile = Instantiate( asset, new Vector3( j * Constants.TILE_SIZE.x, Constants.TILE_SIZE.x, -i * Constants.TILE_SIZE.y ), Quaternion.identity ) as GameObject;
                    tile.transform.parent = worldParent.transform;
                    tile.name = "go_" + index + asset.name;

                    GameObjectUtility.SetStaticEditorFlags( tile, StaticEditorFlags.BatchingStatic );

                    var children = tile.GetComponentsInChildren<Transform>();
                    for ( var k = 0; k < children.Length; k++ )
                    {
                        GameObjectUtility.SetStaticEditorFlags( children[k].gameObject, 
                                                                isRoadPrefab ? StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic : StaticEditorFlags.BatchingStatic );
                    }

                    index++;
                }
            }

            NavMeshBuilder.ClearAllNavMeshes();
            NavMeshBuilder.BuildNavMesh();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene( currentScene );
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetMapFilename( string currentSceneName )
        {
            if ( currentSceneName == null || currentSceneName.Equals( string.Empty ) )
                throw new System.Exception("Scene name is invalid.\nMust contain an underscore and level no., eg. Map_01");

            var substring = currentSceneName.Split( '_' );
            if ( substring.Length < 1 )
                throw new System.Exception( "Scene name is invalid.\nMust contain an underscore and level no., eg. Map_01" );

            var mapIndex = -1;
            if ( !int.TryParse( substring[1], out mapIndex ) )
                throw new System.Exception( "Scene name is invalid.\nMust contain an underscore and level no., eg. Map_01" );

            return @"Map_" + ( mapIndex < 10 ? "0" : "" ) + mapIndex;
        }

        private static XmlDocument GetXMLDocument( string filename )
        {
            var filePath = Application.dataPath + @"/Resources/Maps/" + filename + @".tmx";

            if ( !System.IO.File.Exists( filePath ) )
            {
                throw new System.Exception( "Cannot find " + filename + ".tmx in the Resources/Maps folder" );
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.Load( filePath );

            //save file as xml for Unity Resource.Load to use at the start of a new game
            filePath = Application.dataPath + @"/Resources/Maps/" + filename + @".xml";
            xmlDocument.Save( filePath );

            return xmlDocument;
        }

        private static MapData ReadTMXFileForMapData( string filename )
        {
            return ReadMapDataFromTMXFile( GetXMLDocument( filename ) );
        }

        private static MapData ReadMapDataFromTMXFile( XmlDocument xmlDocument )
        {
            var mapData = new MapData();

            //read metadata
            var xmlData = xmlDocument.DocumentElement.SelectSingleNode( "/map" );
            if ( xmlData == null )
                return null;

            if ( !int.TryParse( xmlData.Attributes["width"].Value.Trim(), out mapData.width ) )
                return null;

            if ( !int.TryParse( xmlData.Attributes["height"].Value.Trim(), out mapData.height ) )
                return null;

            var tilesetsXMLData = xmlDocument.DocumentElement.SelectNodes( "/map/tileset" );

            for ( var i = 0; i < tilesetsXMLData.Count; i++ )
            {
                if ( mapData.tilesetData == null )
                    mapData.tilesetData = new List<TilesetData>();

                var tilesetData = new TilesetData();

                tilesetData.firstgid = int.Parse( tilesetsXMLData[i].Attributes["firstgid"].Value.Trim() );
                tilesetData.prefix = tilesetsXMLData[i].Attributes["name"].Value.Trim();
                tilesetData.tilecount = int.Parse( tilesetsXMLData[i].Attributes["tilecount"].Value.Trim() );

                mapData.tilesetData.Add( tilesetData );
            }

            //read tile map data
            var dataNode = xmlDocument.DocumentElement.SelectSingleNode( "/map/layer[@name='Map']/data" );
            var tiles = dataNode.InnerText.Split( ',' );
            var index = 0;

            mapData.data = new int[mapData.width][];

            for ( var i = 0; i < mapData.width; i++ )
            {
                mapData.data[i] = new int[mapData.height];

                for ( var j = 0; j < mapData.height; j++ )
                {
                    mapData.data[i][j] = int.Parse( tiles[index].Trim() );
                    index++;
                }
            }

            return mapData;
        }

        private static string GetFilepathForWorld( string pathToPrefabsFolder /*= @"Prefabs/TowerDefense"*/, string filename, ref bool isRoadPrefab, ref Vector3 scale )
        {
            isRoadPrefab = false;
            scale = Constants.DEFAULT_LOCAL_SCALE;

            //Roads
            var filepath = pathToPrefabsFolder + "/Roads/" + filename;
            if ( DoesFileExistInResourcesFolder( filepath ) )
            {
                isRoadPrefab = true;
                return filepath;
            }

            ////Buildings
            //filepath = pathToPrefabsFolder + "/Buildings/" + filename;
            //if ( DoesFileExistInResourcesFolder( filepath ) )
            //    return filepath;

            //Grasses
            filepath = pathToPrefabsFolder + "/Grasses/" + filename;
            if ( DoesFileExistInResourcesFolder( filepath ) )
                return filepath;

            ////Rivers
            //filepath = pathToPrefabsFolder + "/Rivers/" + filename;
            //if ( DoesFileExistInResourcesFolder( filepath ) )
            //    return filepath;

            ////Stones
            //filepath = pathToPrefabsFolder + "/Stones/" + filename;
            //if ( DoesFileExistInResourcesFolder( filepath ) )
            //    return filepath;

            ////Trees
            //filepath = pathToPrefabsFolder + "/Trees/" + filename;
            //if ( DoesFileExistInResourcesFolder( filepath ) )
            //    return filepath;

            return string.Empty;
        }

        private static bool DoesFileExistInResourcesFolder( string filepath )
        {
            var fullpath = Application.dataPath + @"/Resources/" + filepath;
            var filename = string.Format( "{0}.{1}", fullpath, "prefab" );
            return System.IO.File.Exists( filename );
        }
    }
}