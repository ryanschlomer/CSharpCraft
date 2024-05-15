
//Is the shadows like they are because e aren't using instanced meshes??


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

    }
}


var scene, camera, renderer;
var controls; // Controls initialized later
var keyStates = {};
var textureLoader = new THREE.TextureLoader();
var defaultMaterial = new THREE.MeshBasicMaterial({ color: 0x000000 }); 


const sun = new THREE.DirectionalLight();
const ambient = new THREE.AmbientLight(0xFFFFFF);
let dayDuration = 60000; // 10 minutes in milliseconds
let halfDay = dayDuration / 2;
let startTime = Date.now();

const blockGeometry = new THREE.BoxGeometry(1, 1, 1);

let textureAtlasData = null; //just one atlas for now.

var stats = {};
let frameCount = 0;
let renderedObjectsCount = 0;
let renderedBlocksCount = 0;

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

function setVoxelData(chunkWidth, chunkHeight) {
    window.voxelData = {
        ChunkWidth: chunkWidth,
        ChunkHeight: chunkHeight
    };
    // You can now use window.voxelData.ChunkWidth and window.voxelData.ChunkHeight as needed
}


function initialize3DScene(canvasId) {
    const canvas = document.getElementById(canvasId);
    scene = new THREE.Scene();
    camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
    renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFShadowMap;
    controls = new THREE.PointerLockControls(camera, canvas);
    document.body.addEventListener('click', () => controls.lock());

    addEventListeners();

    ambient.intensity = 1.0;
    sun.intensity = 0.0;
    sun.position.set(50, 50, 50);
    sun.castShadow = true;

    // Set the size of the sun's shadow box
    sun.shadow.camera.left = -40;
    sun.shadow.camera.right = 40;
    sun.shadow.camera.top = 40;
    sun.shadow.camera.bottom = -40;
    sun.shadow.camera.near = 0.1;
    sun.shadow.camera.far = 200;
    sun.shadow.bias = -0.0001;
    sun.shadow.mapSize = new THREE.Vector2(2048, 2048);

    scene.add(sun);
    scene.add(sun.target);

    const shadowHelper = new THREE.CameraHelper(sun.shadow.camera);
    scene.add(shadowHelper);
 
    
    
    scene.add(ambient);

    scene.fog = new THREE.Fog(0x80a0e0, 75, 100);
    animate();
}


async function applyMaterialsToMesh() {
    for (const mesh of scene.children) {
        if (mesh instanceof THREE.InstancedMesh) {
            const blockType = mesh.userData.blockType;
            const material = await getOrCreateMaterial({ Type: blockType });
            mesh.material = material;
            mesh.material.needsUpdate = true; // Trigger a material update
        }
    }
}




async function getOrCreateMaterial() {
    try {
        // Check if the texture atlas data is already cached
        if (!textureAtlasData) {
            // If not cached, fetch the texture atlas data
            textureAtlasData = await fetchTextureData();
        }
        // Create materials from the cached texture atlas data
        const materials = await createMaterialFromTextures(textureAtlasData);
        return materials;
    } catch (error) {
        console.error("Failed to create material:", error);
        return defaultMaterial;
    }
}





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


async function fetchTextureData() {
    const url = `/api/textures`;
    return fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text(); // Return response as text
        })
        .catch(error => {
            console.error('Error fetching texture data:', error);
            throw error;
        });
}



async function createMaterialFromTextures() {
    try {
        const textureAtlasPath = await fetchTextureData();
        const texture = await loadTextureNearestFilter(textureAtlasPath); // Use the texture atlas path
        //const material = new THREE.MeshBasicMaterial({ map: texture });
        //const material = new THREE.MeshBasicMaterial({ map: texture, side: THREE.FrontSide });
        const material = new THREE.MeshStandardMaterial({ map: texture, side: THREE.FrontSide });
        material.side = THREE.FrontSide;
        material.transparent = false;
        material.depthTest = true;
        material.depthWrite = true;
        
        return material;
    } catch (error) {
        console.error("Failed to create material:", error);
        return defaultMaterial;
    }
}




function clearChunks(chunkIds) {
    updateStats("Before cleanup", scene.children.length);

    console.log(chunkIds);
    if (!scene) {
        return;
    }

    const objectsToRemove = [];

    var s = "";
    scene.traverse((object) => {
        
        if (object.userData.isChunk && chunkIds.includes(object.name)) {
            objectsToRemove.push(object);
            s += object.name + ", ";
        }
    });
    updateStats("Chunk To Remove:", s); // Log before removal

    objectsToRemove.forEach(object => {
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
}







const chunkQueue = new ChunkQueue(); // Queue to store chunks for sequential rendering

function renderChunks(canvasId, jsonUpdatePayload) {
    const updatePayload = JSON.parse(jsonUpdatePayload); // Ensure this parsing is necessary based on how data is received
    //clearChunks(updatePayload.ChunksToRemove);
    console.log(updatePayload);
    updatePayload.forEach(chunk => {
        renderNextChunk(chunk);  // Adjust this if your renderNextChunk can handle direct chunk data
    });
}

async function renderChunk(JSONData) {


    /*
    Some thoughts:
    We aren't using mesh instancing
   
    The youtube three.js guy is not using vertice/triangle/uv lists

    The Youtube guy...how does he add and remove blocks with instance meshes?
    Add Stats object for frame count

    He has each block have an instance id (along with the type of block)

    He has the world data separate from the rendering data. Basically, C# could hold the data; js the mesh.
    Or, I might need to separate the data from the mesh, which maybe I already do

    */
    const maxMeshCount = window.voxelData.ChunkWidth * window.voxelData.ChunkWidth * window.voxelData.ChunkHeight;

    updateStats("scene.children.length", scene.children.length);
    // Parse the JSON string into an object
    const chunkData = JSON.parse(JSONData);

  


    const geometry = new THREE.BufferGeometry();
    const vertices = new Float32Array(chunkData.Vertices.length * 3);

    const uvs = new Float32Array(chunkData.Uvs.length * 2);
    const indices = new Uint32Array(chunkData.Triangles.length);

    // Fill vertices array
    for (let i = 0; i < chunkData.Vertices.length; i++) {
        vertices[i * 3] = chunkData.Vertices[i].x;
        vertices[i * 3 + 1] = chunkData.Vertices[i].y;
        vertices[i * 3 + 2] = chunkData.Vertices[i].z;
    }

    //console.log('Vertices:', vertices);

    // Fill UVs array
    for (let i = 0; i < chunkData.Uvs.length; i++) {
        uvs[i * 2] = chunkData.Uvs[i].x;
        uvs[i * 2 + 1] = chunkData.Uvs[i].y;
    }

    // Fill indices array
    for (let i = 0; i < chunkData.Triangles.length; i++) {
        indices[i] = chunkData.Triangles[i];
    }


    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    geometry.setAttribute('uv', new THREE.BufferAttribute(uvs, 2));
    geometry.setIndex(new THREE.BufferAttribute(indices, 1));

    geometry.computeFaceNormals();
    geometry.computeVertexNormals();


    // hardcoded to 1 for now
    const material = await getOrCreateMaterial() || defaultMaterial; // Replace 'your_block_type_here' with the actual block type

    //maxMeshCount this is set above
    const mesh = new THREE.Mesh(geometry, material);
 
    mesh.userData.isChunk = true; // Set the isChunk property
    
    mesh.name = chunkData.ChunkId;
    mesh.castShadow = true;
    mesh.receiveShadow = true;

    mesh.position.set(chunkData.ChunkX * window.voxelData.ChunkWidth, 0, chunkData.ChunkZ * window.voxelData.ChunkWidth);


    //const normalHelper = new THREE.VertexNormalsHelper(mesh, 1, 0x00ff00);
    //scene.add(normalHelper);


    scene.add(mesh);

    // Optional: continue processing other chunks
   // setTimeout(renderNextChunk, 10);
}





let previousTime = performance.now();

const clock = new THREE.Clock();


let lastSentTime = 0;
const sendInterval = 100; // milliseconds

function animate() {
    //console.log(camera.position);
    
    requestAnimationFrame(animate);
    const now = performance.now();
    const deltaTime = clock.getDelta();

    //if (now - lastSentTime > sendInterval) {
        sendInputToServer(deltaTime);
        lastSentTime = now;
    //}


    //// Call the C# method via SignalR
    //connection.invoke("UpdatePlayer", deltaTime)
    //    .catch(err => console.error(err.toString()));

    //sendCameraPositionToBlazor();

   
    let currentTime1 = performance.now();
    let dt = (currentTime1 - previousTime) / 1000;


   



    //I think all of the rest of the function is for informational purposes only
    //except the renderer call.

    renderedObjectsCount = 0;
    renderedBlocksCount = 0;

    scene.traverse(object => {
        if (object.isMesh) {
            renderedObjectsCount++;
            if (!object.material || object.material.map === undefined) {
                object.material = defaultMaterial; // Fallback to default material
            }
            if (object.isInstancedMesh) {
                renderedBlocksCount += object.count;
            }
        }
    });

    

    // Calculate the elapsed time
    let currentTime = new Date();
    let timeElapsed = (currentTime - startTime) % dayDuration;
    let angle = Math.PI * 2 * (timeElapsed / dayDuration); // Full circle

    // Update sun's position
    sun.position.x = 50 * Math.sin(angle); // 50 is the radius of the sun's circular path
    sun.position.y = 50 * Math.cos(angle);
    sun.position.z = 0; // Keep z constant or adjust for 3D effect

    // Adjust light intensity based on time of day
    //if (timeElapsed < halfDay) {
    //    // Daytime
    //    sun.intensity = 1.5 * (Math.cos(angle) + 1) / 2; // Peaks at noon
    //} else {
    //    // Nighttime
    //    sun.intensity = 0; // Sun is below the horizon, simulate darkness
    //}

    //ambient.intensity = (timeElapsed < halfDay) ? 0.5 : 0.8;

    renderer.render(scene, camera);

    // Log rendering stats every 60 frames
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
    previousTime = currentTime1;

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
        element.requestFullscreen();  // Standard method
    } else if (element.webkitRequestFullscreen) {
        element.webkitRequestFullscreen(); // Chrome, Safari and Opera
    } else if (element.msRequestFullscreen) {
        element.msRequestFullscreen(); // IE/Edge
    }
}



// Define and initialize the connection object at the top of your script
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub")  // Ensure this matches your server configuration
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
        sessionStorage.setItem('connectionId', connectionId);  // Store connection ID in sessionStorage
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


////SignalR stuff
//// Define and initialize the connection object at the top of your script
//const connection = new signalR.HubConnectionBuilder()
//    .withUrl("/gameHub")  // Ensure this matches your server configuration
//    .configureLogging(signalR.LogLevel.Information)
//    .build();

//async function setupConnection() {
//    try {
//        // Start the connection
//        await connection.start();
//        console.log("Connection started");

//        // Get the connection ID from the server
//        const connectionId = await connection.invoke("GetConnectionId");
//        sessionStorage.setItem('connectionId', connectionId);  // Store connection ID in sessionStorage
//        console.log("Connection ID set: " + connectionId);

//        // Optionally, if using Blazor or another framework, notify that the connection is ready
//        if (typeof DotNet !== 'undefined') {
//            await DotNet.invokeMethodAsync('YourAssemblyName', 'ConnectionReady');
//        }
//    } catch (error) {
//        console.error("Error setting up SignalR connection:", error);
//    }
//}




//connection.start().catch(function (err) {
//    console.error("Error while starting connection: " + err.toString());
//});


//connection.on("ReceiveChunkData", function (chunkData) {
//    //console.log("Received chunk data:", chunkData);

//    // Enqueue the received chunk data for processing
//    if (chunkData) {
//        chunkQueue.enqueue(chunkData);
//    }
//});

//connection.on("ChunksToRemove", function (chunkIds) {
//    //console.log("Received Visible Chunk Ids :", chunkIds);

////Keep this the same for now. We might want to change this to populating the queue and processing it.
//    if (chunkIds) {
//        clearChunks(chunkIds);  // Function to process and render the chunk
//    }
//});

//async function updatePlayerChunkData(data) {
//    try {
//        await connection.invoke("UpdatePlayerChunkData", data);
//        console.log("UpdatePlayerChunkData invoked successfully.");
//    } catch (error) {
//        console.error("Error while invoking UpdatePlayerChunkData:", error);
//    }
//}

//// Define the getConnectionId function to retrieve the connection ID
//window.getConnectionId = async () => {
//    try {
//        // Invoke the 'getConnectionId' method on the SignalR hub
//        const connectionId = await connection.invoke("getConnectionId");
//        return connectionId;
//    } catch (error) {
//        console.error("Error while getting connection ID:", error);
//        return null;
//    }
//};

//connection.invoke("GetConnectionId").then(function (connectionId) {
//    sessionStorage.setItem('connectionId', connectionId);  // Store connection ID in sessionStorage
//    console.log("Connection ID set: " + connectionId);
//}).catch(function (err) {
//    return console.error(err.toString());
//});




//// Call setupConnection to initialize everything
//setupConnection();