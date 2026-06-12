// Скачивание файла из памяти браузера без отправки на сервер.
// Получает поток данных из .NET (DotNetStreamReference) и инициирует скачивание.
// Официальный рекомендуемый Microsoft паттерн для Blazor.
window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName ?? '';
    document.body.appendChild(anchor);
    anchor.click();

    // Уборка: удаляем ссылку и освобождаем память.
    anchor.remove();
    URL.revokeObjectURL(url);
};
