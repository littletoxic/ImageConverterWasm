export async function createThumbnailUrl(contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    return URL.createObjectURL(blob);
}

export function revokeThumbnailUrl(url) {
    URL.revokeObjectURL(url);
}

export async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName ?? '';
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
}
