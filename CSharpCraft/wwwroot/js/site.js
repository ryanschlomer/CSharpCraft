
var scene, camera, renderer;
var controls; // Controls initialized later
var keyStates = {};
var textureLoader = new THREE.TextureLoader();
var defaultMaterial = new THREE.MeshBasicMaterial({ color: 0x000000 }); 

var blockTextures = {}; // Stores materials for each block type

const blockGeometry = new THREE.BoxGeometry(1, 1, 1);

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

async function createMaterialFromTextures(textures) {

    try {
        const sideTexture = await loadTextureNearestFilter(textures.side);
        const topTexture = await loadTextureNearestFilter(textures.top);
        const bottomTexture = await loadTextureNearestFilter(textures.bottom);

        return [
            new THREE.MeshBasicMaterial({ map: sideTexture }),
            new THREE.MeshBasicMaterial({ map: sideTexture }),
            new THREE.MeshBasicMaterial({ map: topTexture }),
            new THREE.MeshBasicMaterial({ map: bottomTexture }),
            new THREE.MeshBasicMaterial({ map: sideTexture }),
            new THREE.MeshBasicMaterial({ map: sideTexture })
        ];
    } catch (error) {
        //console.error("Failed to create materials:", error);
        return [defaultMaterial, defaultMaterial, defaultMaterial, defaultMaterial, defaultMaterial, defaultMaterial];
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
    console.log("Chunks to remove:", chunksToRemove);  // Log which chunks are expected to be removed

    if (!scene) {
        console.error("Scene is undefined. Cannot traverse.");
        return;
    }

    const objectsToRemove = [];

    scene.traverse((object) => {
        console.log(`Checking object with name: ${object.name}`); // Log every object being checked
        console.log("Is chunk: ", object.userData.isChunk) //this is null
        console.log("object.name: ", object.name)
        if (object.userData.isChunk && chunksToRemove.includes(object.name)) {
            objectsToRemove.push(object);
            console.log(`Marked for removal: ${object.name}`); // Log when an object is marked for removal
            //The line above is not logging
        }
    });

    console.log(`Identified ${objectsToRemove.length} chunks for removal.`);

    objectsToRemove.forEach(object => {
        if (object.material) {
            if (Array.isArray(object.material)) {
                object.material.forEach(mat => {
                    if (mat.map) {
                        mat.map.dispose();
                        console.log(`Disposed map for material.`);
                    }
                    mat.dispose();
                    console.log(`Disposed material.`);
                });
            } else {
                if (object.material.map) {
                    object.material.map.dispose();
                    console.log(`Disposed single map for material.`);
                }
                object.material.dispose();
                console.log(`Disposed single material.`);
            }
        }

        if (object.geometry) {
            object.geometry.dispose();
            console.log(`Disposed geometry.`);
        }

        // Remove the object from the scene
        scene.remove(object);
        console.log(`Removed object: ${object.name}`);
    });

    console.log(`Removed ${objectsToRemove.length} objects from scene.`);
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


function renderChunks(canvasId, jsonUpdatePayload) {
    const updatePayload = JSON.parse(jsonUpdatePayload);
    clearChunks(updatePayload.ChunksToRemove);


    updatePayload.NewChunks.forEach(chunk => {
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
    const blocksGroupedByType = groupBlocksByType(chunk.Blocks);

    Object.keys(blocksGroupedByType).forEach(async (blockType) => {
        const blocks = blocksGroupedByType[blockType];
        const material = await getOrCreateMaterial({ Type: blockType }) || defaultMaterial;
        const geometry = blockGeometry;
        const instancedMesh = new THREE.InstancedMesh(geometry, material, blocks.length);
        

        blocks.forEach((block, index) => {
            const dummy = new THREE.Object3D();
            dummy.position.set(block.X, block.Y, block.Z);
            dummy.updateMatrix();
            instancedMesh.setMatrixAt(index, dummy.matrix);
        });

        instancedMesh.name = chunk.ChunkId; // This should match the ChunkID format
        console.log("instancedMesh.name: ", instancedMesh.name);
        instancedMesh.userData.blockType = blockType;
        instancedMesh.userData.isChunk = true;
        instancedMesh.instanceMatrix.needsUpdate = true;
        scene.add(instancedMesh);
    });

    // Optional: continue processing other chunks
    setTimeout(renderNextChunk, 10);
}



//let frameCount = 0;
//let renderedObjectsCount = 0;

const axesHelper = new THREE.AxesHelper(5);

const gridHelper = new THREE.GridHelper(100, 10);


function animate() {

    scene.add(gridHelper);
    scene.add(axesHelper);

    requestAnimationFrame(animate);
    updateCameraPosition();

    renderedObjectsCount = 0;

    scene.traverse(object => {
        if (object.isMesh) {
            //renderedObjectsCount++;
            if (!object.material || object.material.map === undefined) {
                object.material = defaultMaterial; // Fallback to default material
            }
        }
    });

    renderer.render(scene, camera);

    sendCameraPositionToBlazor();

    //// Log rendering stats every 60 frames

    //if (frameCount % 60 === 0) {
    //    console.log("Objects rendered in last frame:", renderedObjectsCount);
    //    console.log("Draw calls:", renderer.info.render.calls);
    //    console.log("Triangles rendered:", renderer.info.render.triangles);
    //}

    //frameCount++;
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
