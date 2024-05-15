﻿
var scene, camera, renderer;
var controls; // Controls initialized later
var keyStates = {};
var textureLoader = new THREE.TextureLoader();
var defaultMaterial = new THREE.MeshBasicMaterial({ color: 0x000000 }); 

var blockTextures = {}; // Stores materials for each block type

const blockGeometry = new THREE.BoxGeometry(1, 1, 1);

var stats = {};

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

    controls = new THREE.PointerLockControls(camera, canvas);
    document.body.addEventListener('click', () => controls.lock());

    addEventListeners();


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



async function getOrCreateMaterial(block) {
    if (!block || !block.Type) {
        // If block is null or its 'Type' property is not defined, return default material
        return defaultMaterial;
    }

    if (!blockTextures[block.Type]) {
        // Use default material temporarily
        blockTextures[block.Type] = defaultMaterial;

        try {
            // Fetch texture data
            const textures = await fetchTextureData(block.Type);
            // Create materials from textures
            const materials = await createMaterialFromTextures(textures);
            // Update blockTextures with the newly created materials
            blockTextures[block.Type] = materials;
            // Update scene materials to apply the new materials to existing blocks
            updateSceneMaterials(block.Type);
        } catch (error) {
            // Return default material in case of error
            return defaultMaterial;
        }
    }
    // Return the material for the block (default or loaded)
    return blockTextures[block.Type];
}


function fetchTextureData(type) {
    const url = `/api/textures/${type}`;
    //console.log(`Fetching texture data from: ${url}`);
    return fetch(url)
        .then(response => {
            if (!response.ok) {
                //throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            //console.log('Received texture data:', data);
            return data;
        })
        .catch(error => {
            //console.error('Error fetching texture data:', error);
        });
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
        const material = new THREE.MeshBasicMaterial({ map: texture, side: THREE.FrontSide });

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



function updateSceneMaterials(blockType) {
    scene.traverse(object => {
        if (object.isMesh && object.blockType === blockType) {
            object.material = blockTextures[blockType] || defaultMaterial;
            object.material.needsUpdate = true; // This is critical
        }
    });
}

function clearChunks(chunksToRemove) {
    //console.log("Chunks to remove:", chunksToRemove);  // Log which chunks are expected to be removed
    updateStats("Chunks to remove", chunksToRemove);
    updateStats("Before cleanup", scene.children.length);

    if (!scene) {
        //console.error("Scene is undefined. Cannot traverse.");
        return;
    }

    const objectsToRemove = [];

    scene.traverse((object) => {
       // console.log(`Checking object with name: ${object.name}`); // Log every object being checked
        //console.log("Is chunk: ", object.userData.isChunk) //this is null
        //console.log("object.name: ", object.name)
        if (object.userData.isChunk && chunksToRemove.includes(object.name)) {
            objectsToRemove.push(object);
            //console.log(`Marked for removal: ${object.name}`); // Log when an object is marked for removal
            //The line above is not logging
        }
    });

    //console.log(`Identified ${objectsToRemove.length} chunks for removal.`);

    objectsToRemove.forEach(object => {
        if (object.material) {
            if (Array.isArray(object.material)) {
                object.material.forEach(mat => {
                    if (mat.map) {
                        mat.map.dispose();
                        //console.log(`Disposed map for material.`);
                    }
                    mat.dispose();
                    //console.log(`Disposed material.`);
                });
            } else {
                if (object.material.map) {
                    object.material.map.dispose();
                    //console.log(`Disposed single map for material.`);
                }
                object.material.dispose();
                //console.log(`Disposed single material.`);
            }
        }

        if (object.geometry) {
            object.geometry.dispose();
            //console.log(`Disposed geometry.`);
        }

        // Remove the object from the scene
        scene.remove(object);
        //console.log(`Removed object: ${object.name}`);
    });

    //console.log(`Removed ${objectsToRemove.length} objects from scene.`);
    updateStats("After cleanup", scene.children.length);
}


function modifyMaterial(material) {
    material.onBeforeCompile = function (shader) {
        shader.vertexShader = 'attribute float visibility;\n' + shader.vertexShader;
        shader.vertexShader = shader.vertexShader.replace(
            '#include <begin_vertex>',
            `if (visibility < 0.5) discard;\n#include <begin_vertex>`
        );
    };
}



function populateInstanceBufferAttributes(blocksData, positionAttribute, scaleAttribute) {
    const instanceCount = blocksData.length;
    for (let i = 0; i < instanceCount; i++) {
        const block = blocksData[i];
        if (block) {
            const x = block.X;
            const y = block.Y;
            const z = block.Z;
            positionAttribute.array[i * 3 + 0] = x;
            positionAttribute.array[i * 3 + 1] = y;
            positionAttribute.array[i * 3 + 2] = z;
            scaleAttribute.array[i * 3 + 0] = 1;  // Assuming uniform scale
            scaleAttribute.array[i * 3 + 1] = 1;
            scaleAttribute.array[i * 3 + 2] = 1;
        }
    }
    positionAttribute.needsUpdate = true;
    scaleAttribute.needsUpdate = true;

    //console.log("Meshes prepared:", blocksData.length);
}

function groupBlocksByType(blocks) {
    const groups = {};
    blocks.forEach(block => {
        if (block && block.Type) {
            if (!groups[block.Type]) {
                groups[block.Type] = [];
            }
            groups[block.Type].push(block);
        }
    });
    return groups;
}

const chunkQueue = []; // Queue to store chunks for sequential rendering

let ChunkSize = 0;
let ChunkHeight = 0;




function renderChunks(canvasId, jsonUpdatePayload) {
    const updatePayload = JSON.parse(jsonUpdatePayload);
    clearChunks(updatePayload.ChunksToRemove);

    //console.log(updatePayload);
    updatePayload.NewChunks.forEach(chunk => {
        if (ChunkSize === 0) { //Set the Chunk size and height so we have it
            ChunkSize = chunk.Size;
            ChunkHeight = chunk.Height;
        }

        chunkQueue.push(chunk);
    });

    if (chunkQueue.length === updatePayload.NewChunks.length) {
        renderNextChunk();
        setTimeout(() => {
            applyMaterialsToMesh();  // Delay material application to ensure textures are loaded
        }, 1000); // Adjust delay based on typical load times or dynamically check load status
    }
}



async function renderNextChunk() {
    if (chunkQueue.length === 0) return;

    const chunk = chunkQueue.shift();
    const geometry = new THREE.BufferGeometry();
    const vertices = new Float32Array(chunk.ChunkData.Vertices.length * 3);
    const uvs = new Float32Array(chunk.ChunkData.Uvs.length * 2);
    const indices = new Uint32Array(chunk.ChunkData.Triangles.length);

    // Fill vertices array
    for (let i = 0; i < chunk.ChunkData.Vertices.length; i++) {
        vertices[i * 3] = chunk.ChunkData.Vertices[i].x;
        vertices[i * 3 + 1] = chunk.ChunkData.Vertices[i].y;
        vertices[i * 3 + 2] = chunk.ChunkData.Vertices[i].z;
    }

    //console.log('Vertices:', vertices);

    // Fill UVs array
    for (let i = 0; i < chunk.ChunkData.Uvs.length; i++) {
        uvs[i * 2] = chunk.ChunkData.Uvs[i].x;
        uvs[i * 2 + 1] = chunk.ChunkData.Uvs[i].y;
    }

    // Fill indices array
    for (let i = 0; i < chunk.ChunkData.Triangles.length; i++) {
        indices[i] = chunk.ChunkData.Triangles[i];
    }

    checkVertexOrder(vertices, indices);

    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    geometry.setAttribute('uv', new THREE.BufferAttribute(uvs, 2));
    geometry.setIndex(new THREE.BufferAttribute(indices, 1));

    geometry.computeFaceNormals();
    geometry.computeVertexNormals();

   
   // hardcoded to 1 for now
    const material = await getOrCreateMaterial({ Type: 1 }) || defaultMaterial; // Replace 'your_block_type_here' with the actual block type

    const mesh = new THREE.Mesh(geometry, material);
    mesh.name = chunk.ChunkId;

    //javascript needs to get the VoxelData info eventually.

    mesh.position.set(chunk.ChunkX * window.voxelData.ChunkWidth, 0, chunk.ChunkZ * window.voxelData.ChunkWidth);
    // Set mesh position based on chunk coordinates
    //mesh.position.set(chunk.ChunkX * 5, 0, chunk.ChunkZ * 5);

    const normalHelper = new THREE.VertexNormalsHelper(mesh, 1, 0x00ff00);
    scene.add(normalHelper);


    scene.add(mesh);

    // Optional: continue processing other chunks
    setTimeout(renderNextChunk, 10);
}



function checkVertexOrder(vertices, indices) {
    for (let i = 0; i < indices.length; i += 3) {
        const idx1 = indices[i] * 3;
        const idx2 = indices[i + 1] * 3;
        const idx3 = indices[i + 2] * 3;

        const v1 = new THREE.Vector3(vertices[idx1], vertices[idx1 + 1], vertices[idx1 + 2]);
        const v2 = new THREE.Vector3(vertices[idx2], vertices[idx2 + 1], vertices[idx2 + 2]);
        const v3 = new THREE.Vector3(vertices[idx3], vertices[idx3 + 1], vertices[idx3 + 2]);

        const edge1 = new THREE.Vector3().subVectors(v2, v1);
        const edge2 = new THREE.Vector3().subVectors(v3, v1);
        const normal = new THREE.Vector3().crossVectors(edge1, edge2).normalize();

        console.log(`Triangle ${i / 3}: Normal = (${normal.x.toFixed(2)}, ${normal.y.toFixed(2)}, ${normal.z.toFixed(2)})`);
    }
}



let frameCount = 0;
let renderedObjectsCount = 0;
let renderedBlocksCount = 0;

//const axesHelper = new THREE.AxesHelper(5);
//const gridHelper = new THREE.GridHelper(100, 10);
//scene.add(gridHelper);
//scene.add(axesHelper);



function animate() {
    requestAnimationFrame(animate);
    updateCameraPosition();

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

    renderer.render(scene, camera);
    sendCameraPositionToBlazor();

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

