class ChunkQueue {
    constructor() {
        this.queue = [];
        this.isProcessing = false;
    }

    enqueue(chunkData) {
        this.queue.push(chunkData);
        this.processQueue();
    }

    async processQueue() {
        if (!this.isProcessing && this.queue.length > 0) {
            this.isProcessing = true;
            while (this.queue.length > 0) {
                const chunkData = this.queue.shift();
                await this.processChunk(chunkData);
            }
            this.isProcessing = false;
        }
    }

    async processChunk(chunkData) {
        // Process your chunk data here, e.g., parsing JSON, updating the scene, etc.
        //console.log("Processing chunk:", chunkData);
        renderChunk(chunkData);
        //renderTestBlock(chunkData);
    }
}

window.GetCameraComponent = {
    registerComponent: function (component) {
        window.cameraComponent = component;
        // Once the component is registered, call the updateCameraPosition function
    }
};

function setVoxelData(chunkWidth, chunkHeight) {
    window.voxelData = {
        ChunkWidth: chunkWidth,
        ChunkHeight: chunkHeight
    };
    // You can now use window.voxelData.ChunkWidth and window.voxelData.ChunkHeight as needed
}

var scene, camera, renderer;
var controls; // Controls initialized later
var keyStates = {};
var textureLoader = new THREE.TextureLoader();
var defaultMaterial = new THREE.MeshBasicMaterial({ color: 0x000000 });

var UVs = {}

let textureAtlasData = null; //just one atlas for now.
let textureAtlasMaterial;

var material;

const blockGeometry = new THREE.BoxGeometry(1, 1, 1);
var maxCount = 0;
var uvAttribute;

var stats = {};
let frameCount = 0;
let renderedObjectsCount = 0;
let renderedBlocksCount = 0;

// Initialize Raycaster
const raycaster = new THREE.Raycaster(new THREE.Vector3(), new THREE.Vector3(), 0, 8);
const mouse = new THREE.Vector2(0, 0); // Center of the screen
var highlightMeshPlace, highlightMeshRemove;
var selectedCoords = null;

function updateStats(key, value) {
    stats[key] = value;
}

function statsToString() {
    let statsString = "";
    for (const [key, value] of Object.entries(stats)) {
        statsString += `${key}: ${value}<br>`; // Using <br> to create a new line in HTML
    }
    return statsString;
}

const loader = new THREE.GLTFLoader();
var pickaxeMesh;

async function initialize3DScene(canvasId) {
    console.log("Initializing 3D scene...");
    const canvas = document.getElementById(canvasId);
    maxCount = window.voxelData.ChunkWidth * window.voxelData.ChunkWidth * window.voxelData.ChunkHeight;
    uvAttribute = new THREE.InstancedBufferAttribute(new Float32Array(maxCount * 8 * 6), 2); // 8 UVs per face, 6 faces
    blockGeometry.setAttribute('instanceUV', uvAttribute);
    scene = new THREE.Scene();
    camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
    console.log("Camera initialized:", camera);

    renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    controls = new THREE.PointerLockControls(camera, canvas);
    document.body.addEventListener('click', () => controls.lock());

    //Get UV Data
    await getUVData();

    console.log("Get UV Data Complete");

    // Add ambient light
    const ambientLight = new THREE.AmbientLight(0xAAAAAA); // Soft white light
    scene.add(ambientLight);

    initializeHighlightMeshes();

    addEventListeners();

    //scene.fog = new THREE. Fog(0x80a0e0, 75, 100);

    console.log("Starting animation loop...");

    await initializeTextureAtlas(); // Initialize texture atlas

    scene.add(camera);
    //test coad to load a model:

    loader.load('/Graphics/Models/pickaxe.glb', function (gltf) {
        console.log("Got here", loader);
        pickaxeMesh = gltf.scene;

        camera.add(pickaxeMesh);
        console.log("Pickaxe added to camera");

        // Position the pickaxe in front of the camera
        pickaxeMesh.position.set(.75, -.25, -0.5);
        pickaxeMesh.scale.set(0.5, 0.5, 0.5);
        pickaxeMesh.rotation.z = Math.PI / 2;
        pickaxeMesh.rotation.y = Math.PI + .5;
        pickaxeMesh.rotation.x = 0.2;

        console.log("Pickaxe position:", pickaxeMesh.position);

        pickaxeMesh.updateMatrixWorld();
        const worldPosition = new THREE.Vector3();
        pickaxeMesh.getWorldPosition(worldPosition);
        console.log("Pickaxe world position:", worldPosition);

        const cameraPosition = new THREE.Vector3();
        camera.getWorldPosition(cameraPosition);
        console.log("Camera world position:", cameraPosition);

    }, undefined, function (error) {
        console.error('An error occurred while loading the model', error);
    });

    animate();

}

// Function to fetch UV data from the server
async function getUVData() {
    try {
        const uvData = await connection.invoke("GetUVData");

        // Collect all the loadTextureAtlas promises
        const promises = uvData.map(async block => {
            // Load the texture atlas for the block type
            const material = await loadTextureAtlas(block.textureAtlas);

            // Store the UV data
            UVs[block.blockType] = {
                textureAtlas: block.textureAtlas,
                blockName: block.blockName,
                isSolid: block.isSolid,
                faces: block.faces,
                material: material
            };
        });

        // Wait for all the promises to complete
        await Promise.all(promises);

        // UV data is now fully loaded and stored
        //console.log("UV data received and stored:", JSON.stringify(UVs, null, 2)); // Detailed log of stored data
    } catch (err) {
        console.error(err.toString());
    }
}

//// Function to fetch UV data from the server
//function getUVData() {
// connection.invoke("GetUVData")
// .then(async uvData => {
// //console.log("Received UV data:", JSON.stringify(uvData, null, 2)); // Detailed log of received data

// for (const block of uvData) {
// //console.log(`Processing block type: ${block.blockType}`);

// // Load the texture atlas for the block type
// const material = await loadTextureAtlas(block.textureAtlas);

// UVs[block.blockType] = {
// textureAtlas: block.textureAtlas,
// blockName: block.blockName,
// isSolid: block.isSolid,
// faces: block.faces,
// material: material
// };

// //console.log(`Stored UV data for block type ${block.blockType}:`, JSON.stringify(UVs[block.blockType], null, 2));
// }

// //console.log("UV data received and stored:", JSON.stringify(UVs, null, 2)); // Detailed log of stored data
// })
// .catch(err => console.error(err.toString()));
//}

function loadTextureNearestFilter(imagePath) {
    return new Promise((resolve, reject) => {
        new THREE.TextureLoader().load(imagePath,
            texture => {
                texture.minFilter = THREE.NearestFilter;
                texture.magFilter = THREE.NearestFilter;
                resolve(texture);
            },
            undefined,
            error => reject(error)
        );
    });
}

//function clearChunks(chunkIds) {
// updateStats("Before cleanup", scene.children.length);

// console.log(chunkIds);
// if (!scene) {
// return;
// }

// const objectsToRemove = [];

// var s = "";
// scene.traverse((object) => {

// if (object.userData.isChunk && chunkIds.includes(object.name)) {
// objectsToRemove.push(object);
// s += object.name + ", ";
// }
// });
// updateStats("Chunk To Remove:", s); // Log before removal

// objectsToRemove.forEach(object => {
// if (object.material) {
// if (Array.isArray(object.material)) {
// object.material.forEach(mat => {
// if (mat.map) {
// mat.map.dispose();
// }
// mat.dispose();
// });
// } else {
// if (object.material.map) {
// object.material.map.dispose();
// }
// object.material.dispose();
// }
// }

// if (object.geometry) {
// object.geometry.dispose();
// }

// scene.remove(object);
// });

// updateStats("After cleanup", scene.children.length);
//}

function clearChunks(chunkIds) {//this doesn't quite work
    updateStats("Before cleanup", scene.children.length);

    console.log("Chunk IDs to remove:", chunkIds);
    if (!scene) {
        return;
    }

    const objectsToRemove = [];

    scene.traverse((object) => {
        if (object.userData.isChunk) {
            console.log("Checking object:", object.name);
            if (chunkIds.includes(object.name.split('_')[0])) { // Check only ChunkId part of the name
                console.log("Object to remove found:", object.name);
                objectsToRemove.push(object);
            }
        }
    });

    updateStats("Chunks To Remove:", objectsToRemove.map(obj => obj.name).join(", ")); // Log before removal

    objectsToRemove.forEach(object => {
        // Log details about the object being removed
        console.log("Removing object:", object.name);

        if (object.material) {
            if (Array.isArray(object.material)) {
                object.material.forEach(mat => {
                    if (mat.map) {
                        mat.map.dispose();
                    }
                    mat.dispose();
                });
            } else {
                if (object.material.map) {
                    object.material.map.dispose();
                }
                object.material.dispose();
            }
        }

        if (object.geometry) {
            object.geometry.dispose();
        }

        scene.remove(object);
    });

    updateStats("After cleanup", scene.children.length);
    console.log("Chunks removed:", objectsToRemove.map(obj => obj.name).join(", "));
}

async function initializeTextureAtlas() {
    textureAtlasMaterial = await initializeMaterials();
}

function getMaterialForBlockId(blockId) {
    return new Promise((resolve) => {
        resolve(textureAtlasMaterial || defaultMaterial);
    });
}

async function loadTextureAtlas(path) {
    return new Promise((resolve, reject) => {
        new THREE.TextureLoader().load(path,
            texture => {
                //texture.wrapS = THREE. RepeatWrapping;
                //texture.wrapT = THREE. RepeatWrapping;
                texture.minFilter = THREE.NearestFilter;
                texture.magFilter = THREE.NearestFilter;
                console.log("Texture Atlas Loaded:", texture);
                resolve(texture);
            },
            undefined,
            error => reject(error)
        );
    });
}

async function createMaterialFromTextures(textureAtlas) {
    const material = new THREE.MeshStandardMaterial({ map: textureAtlas, side: THREE.FrontSide });
    material.transparent = false;
    material.depthTest = true;
    material.depthWrite = true;
    return material;
}

async function initializeMaterials() {
    const textureAtlasPath = '/Graphics/Blocks.png';
    const textureAtlas = await loadTextureAtlas(textureAtlasPath);
    if (!textureAtlas) {
        console.error("Texture Atlas failed to load");
        return defaultMaterial; // Fallback to default material
    }

    material = new THREE.MeshBasicMaterial({
        map: textureAtlas
    });
    console.log("Basic Material Created with Texture Atlas:", material);
}

const textureAtlases = {};

// Function to load texture atlases
async function loadTextureAtlas(url) {
    if (!textureAtlases[url]) {
        const texture = await new THREE.TextureLoader().loadAsync(url);
        texture.minFilter = THREE.NearestFilter;
        texture.magFilter = THREE.NearestFilter;
        texture.format = THREE.RGBAFormat; // Ensure texture uses RGBA format
        textureAtlases[url] = new THREE.MeshBasicMaterial({
            map: texture,
            transparent: true, // Enable transparency
            alphaTest: 0.5 // Adjust alpha test threshold as needed
        });
    }
    return textureAtlases[url];
}

const chunkQueue = new ChunkQueue(); // Queue to store chunks for sequential rendering

function renderChunks(canvasId, jsonUpdatePayload) {
    const updatePayload = JSON.parse(jsonUpdatePayload); // Ensure this parsing is necessary based on how data is received
    //clearChunks(updatePayload.ChunksToRemove);
    console.log(updatePayload);
    updatePayload.forEach(chunk => {
        renderNextChunk(chunk); // Adjust this if your renderNextChunk can handle direct chunk data
    });
}

let previousTime = performance.now();

const clock = new THREE.Clock();

let lastSentTime = 0;
const sendInterval = 100; // milliseconds

var a = 0;
var aStep = .01
function animate() {
    requestAnimationFrame(animate);
    const now = performance.now();
    const deltaTime = clock.getDelta();

    sendInputToServer(deltaTime);

    updateRaycaster();
    lastSentTime = now;

    if (a > 1)
        aStep = -.01
    if (a < -1)
        aStep = .01
    a += aStep
    if (camera && pickaxeMesh) {

    }

    renderedObjectsCount = 0;
    renderedBlocksCount = 0;

    scene.traverse(object => {
        if (object.isMesh) {
            renderedObjectsCount++;
            if (!object.material || object.material.map === undefined) {
                console.log("!object.material: ", object.material); //This didn't error
                object.material = defaultMaterial; // Fallback to default material
            }
            if (object.isInstancedMesh) {
                renderedBlocksCount += object.count;
            }
        }
    });

    renderer.render(scene, camera);

    if (frameCount % 60 === 0) {
        updateStats("Objects rendered in last frame", renderedObjectsCount);
        updateStats("Blocks rendered in last frame", renderedBlocksCount);
        updateStats("Draw calls", renderer.info.render.calls);
        updateStats("Triangles rendered", renderer.info.render.triangles);

        const log = document.getElementById('log');
        if (log) {
            log.innerHTML = statsToString();
        }
    }

    frameCount++;
    previousTime = performance.now();

    updateCameraPositionDisplay();
}

function adjustCanvasSize(canvasId) {
    const canvas = document.getElementById(canvasId);
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}

function requestFullScreen(elementId) {
    var element = document.getElementById(elementId);
    if (element.requestFullscreen) {
        element.requestFullscreen(); // Standard method
    } else if (element.webkitRequestFullscreen) {
        element.webkitRequestFullscreen(); // Chrome, Safari and Opera
    } else if (element.msRequestFullscreen) {
        element.msRequestFullscreen(); // IE/Edge
    }
}

// Define and initialize the connection object at the top of your script
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub") // Ensure this matches your server configuration
    .configureLogging(signalR.LogLevel.Information)
    .build();

async function setupConnection() {
    try {
        // Check if the connection is already started
        if (connection.state !== "Connected") {
            await connection.start();
            console.log("Connection started");
        }

        // Get the connection ID from the server after ensuring connection is started
        const connectionId = await connection.invoke("GetConnectionId");
        sessionStorage.setItem('connectionId', connectionId); // Store connection ID in sessionStorage
        console.log("Connection ID set: " + connectionId);

    } catch (error) {
        console.error("Error setting up SignalR connection:", error);
    }
}

// Call setupConnection to initialize everything
setupConnection();

// Event listeners for receiving data
connection.on("ReceiveChunkData", function (chunkData) {
    if (chunkData) {
        chunkQueue.enqueue(chunkData);
    }
});

connection.on("ChunksToRemove", function (chunkIds) {
    if (chunkIds) {
        clearChunks(chunkIds);
    }
});

async function updatePlayerChunkData(data) {
    try {
        // Ensure the connection is ready before invoking a method
        if (connection.state === "Connected") {
            await connection.invoke("UpdatePlayerChunkData", data);
            console.log("UpdatePlayerChunkData invoked successfully.");
        } else {
            console.log("Connection is not ready.");
        }
    } catch (error) {
        console.error("Error while invoking UpdatePlayerChunkData:", error);
    }
}

function getChunkPositionFromId(chunkId) {
    const x = parseInt(chunkId.slice(0, 6), 10); // Extract the first part as chunkX
    const z = parseInt(chunkId.slice(6), 10); // Extract the second part as chunkZ
    return { x, z };
}
async function renderTestBlock() {
    const testUVs = {
        BackFace: [
            { X: 0.0, Y: 0.0 }, { X: 0.25, Y: 0.0 }, { X: 0.25, Y: 0.25 }, { X: 0.0, Y: 0.25 }
        ],
        FrontFace: [
            { X: 0.25, Y: 0.0 }, { X: 0.5, Y: 0.0 }, { X: 0.5, Y: 0.25 }, { X: 0.25, Y: 0.25 }
        ],
        TopFace: [
            { X: 0.5, Y: 0.0 }, { X: 0.75, Y: 0.0 }, { X: 0.75, Y: 0.25 }, { X: 0.5, Y: 0.25 }
        ],
        BottomFace: [
            { X: 0.75, Y: 0.0 }, { X: 1.0, Y: 0.0 }, { X: 1.0, Y: 0.25 }, { X: 0.75, Y: 0.25 }
        ],
        LeftFace: [
            { X: 0.0, Y: 0.25 }, { X: 0.25, Y: 0.25 }, { X: 0.25, Y: 0.5 }, { X: 0.0, Y: 0.5 }
        ],
        RightFace: [
            { X: 0.25, Y: 0.25 }, { X: 0.5, Y: 0.25 }, { X: 0.5, Y: 0.5 }, { X: 0.25, Y: 0.5 }
        ]
    };

    const blockGeometry = new THREE.BoxGeometry(1, 1, 1);
    const uvArray = new Float32Array(4 * 6 * 2); // 4 UVs per face, 6 faces, 2 coordinates per UV

    let offset = 0;

    for (const face of ['BackFace', 'FrontFace', 'TopFace', 'BottomFace', 'LeftFace', 'RightFace']) {
        const uvFace = testUVs[face];
        for (const uvPoint of uvFace) {
            uvArray[offset++] = uvPoint.X;
            uvArray[offset++] = uvPoint.Y;
        }
    }

    // Create the geometry with the UV attribute
    blockGeometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvArray, 2));

    const blockMesh = new THREE.Mesh(blockGeometry, material);
    blockMesh.position.set(0, 0, 0);
    scene.add(blockMesh);
}

const blockInstanceIds = {};

async function renderChunk(JSONData) {

    console.log("render Chunk Start");
    const chunkData = JSON.parse(JSONData);
    const { ChunkId, ChunkX, ChunkZ, Blocks } = chunkData;
    const blockMeshes = {};

    for (const block of Blocks) {
        const { BlockId, Position } = block;

        if (!UVs[BlockId]) {
            //This error occurs a lot??
            console.error(`UV data for block type ${BlockId} not found.`);
            console.log("UVs: ", UVs);
            continue;
        }

        const { material, faces } = UVs[BlockId];

        if (!blockMeshes[BlockId]) {
            const maxCount = window.voxelData.ChunkWidth * window.voxelData.ChunkWidth * window.voxelData.ChunkHeight;
            const blockGeometry = new THREE.BoxGeometry(1, 1, 1);
            const uvArray = new Float32Array(maxCount * 4 * 6 * 2); // 4 UVs per face, 6 faces, 2 coordinates per UV

            blockGeometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvArray, 2));

            const instancedMesh = new THREE.InstancedMesh(blockGeometry, material, maxCount);
            instancedMesh.name = `${ChunkId}_${BlockId}`;
            instancedMesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
            instancedMesh.count = 0;
            blockMeshes[BlockId] = instancedMesh;
        }

        const instancedMesh = blockMeshes[BlockId];
        const index = instancedMesh.count++;

        // Calculate global positions
        const globalX = Position.X + ChunkX * window.voxelData.ChunkWidth;
        const globalZ = Position.Z + ChunkZ * window.voxelData.ChunkWidth;
        const globalY = Position.Y;

        const blockKey = `${globalX}_${globalY}_${globalZ}`;

        // Check for duplicate block instance
        if (blockInstanceIds[blockKey]) {
            instancedMesh.count--; // Revert the count increment
            continue; // Skip adding this block
        }

        // Adjust positions
        const adjustedX = globalX + 0.5;
        const adjustedZ = globalZ + 0.5;
        const adjustedY = globalY + 0.5;

        const matrix = new THREE.Matrix4().makeTranslation(adjustedX, adjustedY, adjustedZ);
        instancedMesh.setMatrixAt(index, matrix);

        // Store the instance ID in blockInstanceIds
        blockInstanceIds[blockKey] = { mesh: instancedMesh, instanceId: index };

        // Set UV data for each face
        const uvArray = instancedMesh.geometry.attributes.uv.array;
        let offset = index * 4 * 6 * 2; // Starting offset for this instance's UVs

        const faceNames = ['BackFace', 'FrontFace', 'TopFace', 'BottomFace', 'LeftFace', 'RightFace'];
        faceNames.forEach(face => {
            const uvFace = faces[face];
            uvFace.forEach(uvPoint => {
                uvArray[offset++] = uvPoint.x;
                uvArray[offset++] = uvPoint.y;
            });
        });

        instancedMesh.instanceMatrix.needsUpdate = true;
        instancedMesh.geometry.attributes.uv.needsUpdate = true;
    }

    for (const mesh of Object.values(blockMeshes)) {
        scene.add(mesh);
    }

    updateStats("scene.children.length", scene.children.length);
    console.log("Chunk ID:", ChunkId);
}

function initializeHighlightMeshes() {
    const highlightMaterialPlace = new THREE.MeshBasicMaterial({ color: 0x00ff00, opacity: 0.5, transparent: true });
    const highlightMaterialRemove = new THREE.MeshBasicMaterial({ color: 0xff0000, opacity: 0.5, transparent: true });

    const highlightGeometry = new THREE.BoxGeometry(1.05, 1.05, 1.05);

    highlightMeshPlace = new THREE.Mesh(highlightGeometry, highlightMaterialPlace);
    highlightMeshRemove = new THREE.Mesh(highlightGeometry, highlightMaterialRemove);

    highlightMeshPlace.visible = false; // Initially hide the highlight meshes
    highlightMeshRemove.visible = false;

    scene.add(highlightMeshPlace);
    scene.add(highlightMeshRemove);
}

const CENTER_SCREEN = { x: 0, y: 0 }; // Center of the screen

function getChunkPositionFromId(chunkId) {
    // Example chunk ID: +00000-00001_1
    const chunkCoordinates = chunkId.split('_')[0];
    const chunkX = parseInt(chunkCoordinates.slice(0, 6), 10);
    const chunkZ = parseInt(chunkCoordinates.slice(6), 10);
    return { x: chunkX, z: chunkZ };
}

function removeBlock() {

   

    //if selecedBlock is not null
    if (selectedCoords) {//verify block is selected
        var x = selectedCoords.x - .5;
        var y = selectedCoords.y - .5;
        var z = selectedCoords.z - .5;

        console.log("Got to removeblock", x, y, z);

        connection.invoke("HandleBlockInteraction", x, y, z, "REMOVE")
            .catch(err => console.error(err.toString()));
    }
}

// Function to handle block updates from the server
connection.on("UpdateBlocks", (blocksToUpdate) => {
    console.log("Blocks update received:", blocksToUpdate);

    // Update the blocks in the local scene
    blocksToUpdate.forEach(block => {
        const { x, y, z, blockType, chunkId } = block;
        if (blockType === 0) {
            // Remove block (or set to air)
            removeBlockInstance(x, y, z);
        } else {
            // Add or update block

            //need to figure out why the new block isn't visible
            //and why the screen flashes.
            console.log("x, y, z, blockType, chunkId:", x, y, z, blockType, chunkId);
            addBlockInstance(x, y, z, blockType, chunkId);
        }
    });
});

function updateRaycasterObjects() {
    if (!scene) {
        console.error("Scene is not initialized");
        return;
    }

    if (!raycaster) {
        console.error("Raycaster is not initialized");
        return;
    }

    highlightMeshPlace.visible = false;
    highlightMeshRemove.visible = false;
    selectedCoords = null;

    // Update the list of objects for raycaster to intersect
    raycaster.objects = scene.children.filter(child => child instanceof THREE.InstancedMesh);
    //console.log("Raycaster objects updated:", raycaster.objects);
}

function removeBlockInstance(x, y, z) {
    const blockKey = `${x}_${y}_${z}`;
    const blockData = blockInstanceIds[blockKey];

    if (!blockData) {
        return;
    }

    const { mesh, instanceId } = blockData;

    if (!(mesh instanceof THREE.InstancedMesh)) {
        console.error("Mesh is not an instance of THREE. InstancedMesh");
        return;
    }

    console.log("removeBlockInstance():");
    console.log("BlockKey:", blockKey);
    //console.log("BlockData:", blockData);
    //console.log("Mesh Information:");
    //console.log("Mesh Name:", mesh.name);
    //console.log("Mesh Type:", mesh.type);
    //console.log("Mesh Material:", mesh.material);
    //console.log(`Instance count before removal: ${mesh.count}`);

    if (instanceId >= mesh.count || instanceId < 0) {
        console.error(`Instance ID ${instanceId} is out of bounds`);
        return;
    }

    const lastIndex = mesh.count - 1;

    if (instanceId !== lastIndex) {
        // Get the matrix of the instance to be removed
        const removedMatrix = new THREE.Matrix4();
        mesh.getMatrixAt(instanceId, removedMatrix);

        // Get the matrix of the last instance
        const lastMatrix = new THREE.Matrix4();
        mesh.getMatrixAt(lastIndex, lastMatrix);

        //console.log("Matrix of instance to be removed (before swap):", removedMatrix.elements);
        //console.log("Matrix of last instance (before swap):", lastMatrix.elements);
        //console.log("Index positions swapped:", instanceId, lastIndex);

        // Swap the matrix of the last instance with the matrix of the instance to be removed
        mesh.setMatrixAt(instanceId, lastMatrix);
        mesh.setMatrixAt(lastIndex, removedMatrix);

        // Track keys to avoid redundant updates
        let lastInstanceIdKey = null;

        // Find the key for the last instance in the same mesh
        for (let key in blockInstanceIds) {
            if (blockInstanceIds[key].mesh === mesh && blockInstanceIds[key].instanceId === lastIndex) {
                lastInstanceIdKey = key;
                break;
            }
        }

        // Update the instance ID
        if (lastInstanceIdKey) {
            blockInstanceIds[lastInstanceIdKey].instanceId = instanceId;
            //console.log("Updated instance ID for last instance:", lastInstanceIdKey);
        }
    } else {
        //console.log("Removing last instance directly:", instanceId);
    }

    // Decrease the instance count
    mesh.count--;

    //console.log(`Instance count after removal: ${mesh.count}`);

    // Notify the instanced mesh we updated the instance matrix
    mesh.instanceMatrix.needsUpdate = true;

    // Update the bounding box and bounding sphere
    mesh.geometry.computeBoundingBox();
    mesh.geometry.computeBoundingSphere();

    // Remove the block from blockInstanceIds
    delete blockInstanceIds[blockKey];

    //console.log(`Block at ${x}, ${y}, ${z} removed successfully.`);

    // Update raycaster objects
    updateRaycasterObjects();
}



function addBlockInstance(x, y, z, blockId, chunkId) {
    const blockKey = `${x}_${y}_${z}`;

    if (blockInstanceIds[blockKey]) {
        return;
    }
    console.log("Enter Add Block Instance");

    let instancedMesh = null;
    for (let child of scene.children) {
        if (child.isInstancedMesh && child.name === `${chunkId}_${blockId}`) {
            instancedMesh = child;
            break;
        }
    }

    if (!instancedMesh) {
        console.log("Instanced Mesh does not exist.");

        const maxCount = window.voxelData.ChunkWidth * window.voxelData.ChunkWidth * window.voxelData.ChunkHeight;
        const blockGeometry = new THREE.BoxGeometry(1, 1, 1);
        const uvArray = new Float32Array(maxCount * 4 * 6 * 2); // 4 UVs per face, 6 faces, 2 coordinates per UV
        blockGeometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvArray, 2));

        // Ensure you fetch the material and faces from UVs[blockId]
        const { material, faces } = UVs[blockId];
        instancedMesh = new THREE.InstancedMesh(blockGeometry, material, maxCount);
        instancedMesh.name = `${chunkId}_${blockId}`;
        instancedMesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
        instancedMesh.count = 0;
        scene.add(instancedMesh);
        console.log("Finished adding new mesh.");
    }

    const index = instancedMesh.count++;

    const adjustedX = x + 0.5;
    const adjustedZ = z + 0.5;
    const adjustedY = y + 0.5;

    const matrix = new THREE.Matrix4().makeTranslation(adjustedX, adjustedY, adjustedZ);
    instancedMesh.setMatrixAt(index, matrix);

    blockInstanceIds[blockKey] = { mesh: instancedMesh, instanceId: index };

    const uvArray = instancedMesh.geometry.attributes.uv.array;
    let offset = index * 4 * 6 * 2;

    const faces = UVs[blockId].faces; // Get UVs for the block type
    const faceNames = ['BackFace', 'FrontFace', 'TopFace', 'BottomFace', 'LeftFace', 'RightFace'];

    faceNames.forEach(face => {
        const uvFace = faces[face];
        uvFace.forEach(uvPoint => {
            uvArray[offset++] = uvPoint.x;
            uvArray[offset++] = uvPoint.y;
        });
    });

    instancedMesh.instanceMatrix.needsUpdate = true;
    instancedMesh.geometry.attributes.uv.needsUpdate = true;

    console.log(`Block at ${x}, ${y}, ${z} added successfully.`);
}



let selectedNormal = null;

function updateRaycaster(event) {
    if (!camera) {
        console.error("Camera is not initialized");
        return;
    }

    // Ensure the camera's matrices are up-to-date
    camera.updateMatrixWorld(true);

    if (!scene) {
        console.error("Scene is not initialized");
        return;
    }

    // Ensure the raycaster is initialized
    if (!raycaster) {
        console.error("Raycaster is not initialized");
        return;
    }

    try {
        raycaster.setFromCamera(mouse, camera);

        // Perform the intersection
        const intersects = raycaster.intersectObjects(scene.children, true);

        if (intersects.length > 0) {
            // Find the closest intersected object
            let closestIntersection = null;
            let intersection = null;

            // Iterate through the intersections to find a valid one
            for (let i = 0; i < intersects.length; i++) {
                intersection = intersects[i];

                // Skip highlight meshes
                if (intersection.object === highlightMeshPlace || intersection.object === highlightMeshRemove) {
                    //console.log("Ignoring highlight mesh:", intersection.object);
                    continue;
                }

                // Only get the intersection if it's an InstancedMesh
                if (intersection.object instanceof THREE.InstancedMesh) {
                    closestIntersection = intersection;
                    //console.log("Closest intersection:", closestIntersection);
                    break;
                }
            }

            intersection = closestIntersection;

            // Check if the intersected object is an instance of THREE. InstancedMesh
            if (intersection.object instanceof THREE.InstancedMesh) {
                // Get the transformation matrix of the intersected instance
                const blockMatrix = new THREE.Matrix4();
                intersection.object.getMatrixAt(intersection.instanceId, blockMatrix);

                // Extract the position from the transformation matrix
                const blockPosition = new THREE.Vector3().setFromMatrixPosition(blockMatrix);
                //console.log("Block position:", blockPosition);

                // Extract the chunk ID from the intersected object
                const chunkId = intersection.object.name;
                //console.log("Chunk ID:", chunkId);

                // Calculate the global position by adding the chunk's position
                const chunkPosition = getChunkPositionFromId(chunkId);
                //console.log("Chunk position:", chunkPosition);

                // Adjust block position to the block grid
                const blockX = Math.floor(blockPosition.x);
                const blockY = Math.floor(blockPosition.y);
                const blockZ = Math.floor(blockPosition.z);

                const centerX = blockX + 0.5;
                const centerY = blockY + 0.5;
                const centerZ = blockZ + 0.5;

                // Log the calculated block position
                //console.log("Calculated block center:", { x: centerX, y: centerY, z: centerZ });

                // Center the highlight mesh over the block
                highlightMeshPlace.position.set(centerX, centerY, centerZ);
                highlightMeshPlace.visible = true;
                highlightMeshRemove.visible = false;
                selectedCoords = new THREE.Vector3(centerX, centerY, centerZ);

                // Store the normal of the intersected face
                selectedNormal = intersection.face.normal.clone();

                //console.log("intersection.object that works: ", intersection.object);
            } else {
                // Handle the case where the intersected object is not an instanced mesh
                //console.error("Intersected object is not an instance of THREE. InstancedMesh");
                //console.log("intersection.object: ", intersection.object);
            }
        } else {
            highlightMeshPlace.visible = false;
            highlightMeshRemove.visible = false;
            selectedCoords = null;
            selectedNormal = null;
        }
    } catch (error) {
        // This error occurs a lot
        //console.error("Error updating raycaster:", error);
    }
}