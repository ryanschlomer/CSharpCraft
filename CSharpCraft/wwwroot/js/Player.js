var PlayerHotBar = [];
var selectedSlotIndex = -1; // Track the selected slot index
var PlayerSelectedItem = null;

async function getPlayerHotbarItems() {
    try {
        

        PlayerHotBar = await connection.invoke("GetPlayerHotbarItems");

        console.log("PlayerhotBar: ", PlayerHotBar);

        await getSelectedItemFromServer();

        renderHotbar(PlayerHotBar);

    } catch (err) {
        console.error("Error getPlayerHotbarItems(): ", err.toString());
    }
}

function renderHotbar(hotbarItems) {
    const hotbarContainer = document.getElementById("quickSelectBar");
    hotbarContainer.innerHTML = ''; // Clear existing items

    const atlasSize = 64; // hardcode for now
    const imagesPerRow = 4;
    const imgSize = atlasSize / imagesPerRow;
    const slotSize = 64;

    hotbarItems.forEach((item, index) => {
        const slot = document.createElement("div");
        slot.className = "quick-select-slot";
        slot.id = `quick-select-slot${index + 1}`; // Assign unique ID to each slot
        slot.style.width = '64px';
        slot.style.height = '64px';

        if (item.textureAtlas) {
            const img = document.createElement("img");
            img.src = item.textureAtlas;

            const imageWidthHeight = 400;
            const top = -Math.floor(item.textureAtlasPosition / imagesPerRow) * 62;
            const left = -(item.textureAtlasPosition % imagesPerRow) * 62;

            console.log(top, left);

            img.style.width = `${imageWidthHeight}%`;
            img.style.height = `${imageWidthHeight}%`;
            img.style.position = 'absolute';
            img.style.top = `${top}px`;
            img.style.left = `${left}px`;
            img.style.imageRendering = 'pixelated';

            slot.appendChild(img);
        } else {
            slot.innerText = index + 1;
        }

        // Add click event listener to each slot
        slot.addEventListener('click', () => selectSlot(index));

        hotbarContainer.appendChild(slot);
    });

    // Highlight the selected slot if it exists
    if (selectedSlotIndex !== -1) {
        const selectedSlot = hotbarContainer.children[selectedSlotIndex];
        if (selectedSlot) {
            selectedSlot.classList.add('selected');
        }
    }
}

function selectSlot(index) {
    const hotbarContainer = document.getElementById("quickSelectBar");

    // Remove the selected class from the previously selected slot
    if (selectedSlotIndex !== -1) {
        const previousSlot = hotbarContainer.children[selectedSlotIndex];
        if (previousSlot) {
            previousSlot.classList.remove('selected');
        }
    }

    // Add the selected class to the clicked slot
    const newSlot = hotbarContainer.children[index];
    if (newSlot) {
        newSlot.classList.add('selected');
    }

    // Update the selected slot index
    selectedSlotIndex = index;

    // Send the selected item to the server
    setSelectedItemToServer(index+1);
}

async function setSelectedItemToServer(selectedItemKey) {
    try {
        // Assuming the server returns the selected item
        PlayerSelectedItem = await connection.invoke("SetCurrentItemByKey", selectedItemKey);
        await PlayerSelectedItemChanged();

        console.log("Selected item sent to server and returned: ", PlayerSelectedItem);
    } catch (err) {
        console.error("Error setSelectedItemToServer(): ", err.toString());
    }
}

async function getSelectedItemFromServer() {
    try {
        // Assuming the server returns the selected item
        PlayerSelectedItem = await connection.invoke("GetCurrentItem");
        await PlayerSelectedItemChanged();
        console.log("Get Selected Item: ", PlayerSelectedItem);
    } catch (err) {
        console.error("Error getSelectedItemToServer(): ", err.toString());
    }
}


var selectedItemMesh;

async function PlayerSelectedItemChanged() {
    // Remove selectedItemMesh from camera
    if (selectedItemMesh) {
        camera.remove(selectedItemMesh);
    }

    if (PlayerSelectedItem.type === "Tool") {
        selectedItemMesh = await LoadTool(PlayerSelectedItem.modelPath);
        camera.add(selectedItemMesh);
    } else if (PlayerSelectedItem.type === "Block") {
        selectedItemMesh = createBlockMesh(PlayerSelectedItem);
        camera.add(selectedItemMesh);
    } else if (PlayerSelectedItem.type === "Empty") {
        selectedItemMesh = null; // Clear the selectedItemMesh if the type is Empty
    }
}

async function LoadTool(modelPath) {
    var mesh = await LoadGLBModel(modelPath);
    console.log("modelPath: ", modelPath);
    console.log("mesh: ", mesh);

    // Position the pickaxe in front of the camera
    mesh.position.set(0.75, -0.25, -0.5);
    mesh.scale.set(0.5, 0.5, 0.5);
    mesh.rotation.z = Math.PI / 2;
    mesh.rotation.y = Math.PI + 0.5;
    mesh.rotation.x = 0.2;
    
    

    console.log("mesh mesh: ", mesh);
    // Ensure the mesh materials are rendered last
    mesh.traverse((node) => {
        if (node.isMesh) {
            console.log("mesh: ", node);
            node.material = node.material.clone(); // Clone material to avoid affecting other objects
            node.material.depthTest = true; // Keep depth testing enabled
            node.material.depthWrite = true; // Ensure depth writing is enabled

        } 
    });


    return mesh;
}



function LoadGLBModel(modelPath) {
    return new Promise((resolve, reject) => {
        loader.load(modelPath, function (gltf) {
            var mesh = gltf.scene;
            console.log("mesh in loader: ", mesh);

            // Perform any additional operations on the mesh if necessary
            mesh.updateMatrixWorld();

            // Resolve the promise with the loaded mesh
            resolve(mesh);
        }, undefined, function (error) {
            console.error('An error occurred while loading the model', error);
            // Reject the promise if an error occurs
            reject(error);
        });
    });
}


function createBlockMesh(selectedItem) {
    const { blockId, textureAtlas, textureAtlasPosition } = selectedItem;

    if (!UVs[blockId]) {
        console.error(`UV data for block type ${blockId} not found.`);
        return null;
    }

    const { material, faces } = UVs[blockId];

    const geometry = new THREE.BoxGeometry(1, 1, 1);

    const uvArray = new Float32Array(geometry.attributes.uv.array.length);
    const uvAttribute = new THREE.BufferAttribute(uvArray, 2);

    // Set UV data for each face
    const faceNames = ['BackFace', 'FrontFace', 'TopFace', 'BottomFace', 'LeftFace', 'RightFace'];
    let offset = 0;

    faceNames.forEach(face => {
        const uvFace = faces[face];
        uvFace.forEach(uvPoint => {
            uvAttribute.array[offset++] = uvPoint.x;
            uvAttribute.array[offset++] = uvPoint.y;
        });
    });

    geometry.setAttribute('uv', uvAttribute);

    // Clone the material to avoid affecting other blocks
    const customMaterial = material.clone();
    customMaterial.depthTest = false;
    customMaterial.depthWrite = false;

    const mesh = new THREE.Mesh(geometry, customMaterial);
    mesh.position.set(0.25, -0.25, -0.5); // Adjust position as needed
    mesh.scale.set(0.25, 0.25, 0.25); // Adjust scale as needed

   

    //mesh.position.set(.75, -.25, -0.5);
    //mesh.scale.set(0.5, 0.5, 0.5);
    //mesh.rotation.z = Math.PI / 2;
    //mesh.rotation.y = Math.PI + .5;
    //mesh.rotation.x = 0.2;

    return mesh;
}