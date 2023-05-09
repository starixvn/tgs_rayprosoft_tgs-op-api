interface DeliveryLetter {
    id: string;
    sender: string;
    receiverType: string;
    receiver?: {};
    subject: string;
    content: string;
    comeFrom?: string;
    sentOn: Date;
}