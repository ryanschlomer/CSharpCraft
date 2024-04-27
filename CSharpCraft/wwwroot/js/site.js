
var scene, camera, renderer;
var controls; // Controls initialized later
var keyStates = {};
var textureLoader = new THREE.TextureLoader();
var defaultMaterial = new THREE.MeshBasicMaterial({ color: 0xFFFFFF }); // White material

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


async function getOrCreateMaterial(block) {
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
            //console.error("Error loading textures for block type:", block.Type, error);
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
    //console.log("Chunks to remove:", chunksToRemove);

    if (!scene) {
        //console.error("Scene is undefined. Cannot traverse.");
        return;
    }

    const objectsToRemove = [];

    scene.traverse((object) => {
        if (object.userData.isChunk && chunksToRemove.includes(object.name)) {
            objectsToRemove.push(object);
        }
    });

    objectsToRemove.forEach(object => {
        // Dispose of the material and textures if they exist
        if (object.material) {
            if (Array.isArray(object.material)) {
                // For objects with multiple materials
                object.material.forEach(mat => {
                    if (mat.map) mat.map.dispose();
                    mat.dispose();
                });
            } else {
                // For objects with a single material
                if (object.material.map) object.material.map.dispose();
                object.material.dispose();
            }
        }

        // Dispose of the geometry
        if (object.geometry) {
            object.geometry.dispose();
        }

        // Finally, remove the object from the scene
        scene.remove(object);
    });

    //console.log("Removed objects:", objectsToRemove.length);
}


const chunkQueue = []; // Queue to store chunks for sequential rendering

function renderChunks(canvasId, jsonUpdatePayload) {
    const updatePayload = JSON.parse(jsonUpdatePayload);
    clearChunks(updatePayload.ChunksToRemove); // Remove old chunks based on IDs

    // Enqueue new chunks for rendering
    updatePayload.NewChunks.forEach(chunk => {
        chunkQueue.push(chunk);
    });

    // If no rendering is ongoing, start rendering the next chunk
    if (chunkQueue.length === updatePayload.NewChunks.length) {
        renderNextChunk();
    }
}

async function renderNextChunk() {
    if (chunkQueue.length === 0) return; // No more chunks to render

    const chunk = chunkQueue.shift(); // Dequeue the next chunk
    const chunkSize = chunk.Size;

    // Loop through all blocks within the chunk
    for (let index = 0; index < chunk.Blocks.length; index++) {
        const block = chunk.Blocks[index];

        if (block) {
            const x = index % chunkSize;
            const y = Math.floor(index / (chunkSize * chunkSize));
            const z = Math.floor((index % (chunkSize * chunkSize)) / chunkSize);

            // Create or get material for the block
            const material = await getOrCreateMaterial(block);


            // Create mesh for the block
            const mesh = new THREE.Mesh(blockGeometry, material);
            mesh.position.set(x + chunk.ChunkX * chunkSize, y, z + chunk.ChunkZ * chunkSize);
            mesh.blockType = block.Type;
            mesh.name = chunk.ChunkId; // Use Chunk ID as the name for easier removal

            mesh.userData.isChunk = true;

            mesh.geometry.computeBoundingSphere();


            scene.add(mesh);

            if (blockTextures[block.Type] !== defaultMaterial) {
                mesh.material = blockTextures[block.Type];
                mesh.material.needsUpdate = true;
            }
        }
    }

    // Render the next chunk after a short delay to prevent blocking the main thread
    setTimeout(renderNextChunk, 10);
}



function animate() {
    requestAnimationFrame(animate);
    updateCameraPosition();

    scene.traverse(object => {
        if (object.isMesh) {
            if (!object.material || object.material.map === undefined) {
                object.material = defaultMaterial; // Fallback to default material
            }
        }
    });

    renderer.render(scene, camera);

    sendCameraPositionToBlazor();
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
